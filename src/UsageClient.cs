using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace CodexQuotaWidget
{
    internal sealed class UsageClient
    {
        private const string UsageUrl = "https://chatgpt.com/backend-api/wham/usage";
        private readonly JavaScriptSerializer serializer = new JavaScriptSerializer();

        public Task<UsageSnapshot> FetchAsync()
        {
            return Task.Run(() => Fetch());
        }

        private UsageSnapshot Fetch()
        {
            try
            {
                string authPath = FindAuthFile();
                if (authPath == null)
                    return UsageSnapshot.Error("未找到 Codex 登录信息");

                Dictionary<string, object> auth = ReadObject(authPath);
                Dictionary<string, object> tokens = GetObject(auth, "tokens");
                string accessToken = GetString(tokens, "access_token");
                string accountId = GetString(tokens, "account_id");
                if (String.IsNullOrWhiteSpace(accessToken))
                    return UsageSnapshot.Error("Codex 登录令牌不可用");

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(UsageUrl);
                request.Method = "GET";
                request.Accept = "application/json";
                request.UserAgent = "CodexQuotaWidget/0.3";
                request.Timeout = 15000;
                request.ReadWriteTimeout = 15000;
                request.Headers[HttpRequestHeader.Authorization] = "Bearer " + accessToken;
                if (!String.IsNullOrWhiteSpace(accountId))
                    request.Headers["ChatGPT-Account-Id"] = accountId;

                string json;
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                    json = reader.ReadToEnd();

                return ParseUsage(json);
            }
            catch (WebException ex)
            {
                HttpWebResponse response = ex.Response as HttpWebResponse;
                if (response != null && (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden))
                    return UsageSnapshot.Error("登录已失效，请打开 Codex 重新登录");
                return UsageSnapshot.Error("暂时无法连接用量服务");
            }
            catch (Exception)
            {
                return UsageSnapshot.Error("读取用量失败");
            }
        }

        private UsageSnapshot ParseUsage(string json)
        {
            Dictionary<string, object> root = serializer.DeserializeObject(json) as Dictionary<string, object>;
            Dictionary<string, object> rateLimit = GetObject(root, "rate_limit");
            Dictionary<string, object> primary = GetObject(rateLimit, "primary_window");
            Dictionary<string, object> secondary = GetObject(rateLimit, "secondary_window");
            if (rateLimit == null || (primary == null && secondary == null))
                return UsageSnapshot.Error("用量数据格式已变化");

            Dictionary<string, object> credits = GetObject(root, "credits");
            Dictionary<string, object> resetCredits = GetObject(root, "rate_limit_reset_credits");
            RateWindow fiveHour = new RateWindow();
            RateWindow weekly = new RateWindow();
            ClassifyWindow(primary, ref fiveHour, ref weekly);
            ClassifyWindow(secondary, ref fiveHour, ref weekly);
            if (!fiveHour.IsAvailable && !weekly.IsAvailable)
                return UsageSnapshot.Error("用量窗口类型暂不支持");

            string status = "已连接";
            if (!fiveHour.IsAvailable && weekly.IsAvailable)
                status = "已连接 · 当前仅有一周限额";
            else if (fiveHour.IsAvailable && !weekly.IsAvailable)
                status = "已连接 · 当前仅有短期限额";

            RateWindow sparkFiveHour = new RateWindow();
            RateWindow sparkWeekly = new RateWindow();
            Dictionary<string, object> additional = FindAdditionalRateLimit(root, "GPT-5.3-Codex-Spark");
            if (additional != null)
            {
                Dictionary<string, object> sparkRateLimit = GetObject(additional, "rate_limit");
                ClassifyWindow(GetObject(sparkRateLimit, "primary_window"), ref sparkFiveHour, ref sparkWeekly);
                ClassifyWindow(GetObject(sparkRateLimit, "secondary_window"), ref sparkFiveHour, ref sparkWeekly);
            }

            return new UsageSnapshot
            {
                IsOnline = true,
                StatusMessage = status,
                PlanType = GetString(root, "plan_type"),
                CreditsBalance = GetString(credits, "balance"),
                AvailableResetCredits = GetInt(resetCredits, "available_count"),
                UpdatedAt = DateTime.Now,
                FiveHour = fiveHour,
                Weekly = weekly,
                SparkFiveHour = sparkFiveHour,
                SparkWeekly = sparkWeekly
            };
        }

        private static Dictionary<string, object> FindAdditionalRateLimit(Dictionary<string, object> root, string limitName)
        {
            if (root == null || !root.ContainsKey("additional_rate_limits")) return null;
            object[] limits = root["additional_rate_limits"] as object[];
            if (limits == null) return null;
            foreach (object item in limits)
            {
                Dictionary<string, object> limit = item as Dictionary<string, object>;
                if (String.Equals(GetString(limit, "limit_name"), limitName, StringComparison.OrdinalIgnoreCase))
                    return limit;
            }
            return null;
        }

        private static void ClassifyWindow(Dictionary<string, object> value, ref RateWindow fiveHour, ref RateWindow weekly)
        {
            if (value == null) return;
            long seconds = GetLong(value, "limit_window_seconds");
            RateWindow parsed = ParseWindow(value);

            // The backend may place either window in primary_window. Classify by duration.
            if (seconds >= 6 * 24 * 60 * 60)
                weekly = parsed;
            else if (seconds > 0 && seconds <= 24 * 60 * 60)
                fiveHour = parsed;
        }

        private static RateWindow ParseWindow(Dictionary<string, object> value)
        {
            return new RateWindow
            {
                IsAvailable = true,
                UsedPercent = GetInt(value, "used_percent"),
                ResetAfterSeconds = GetLong(value, "reset_after_seconds"),
                ResetAtUnix = GetLong(value, "reset_at")
            };
        }

        private Dictionary<string, object> ReadObject(string path)
        {
            string json = File.ReadAllText(path, Encoding.UTF8);
            return serializer.DeserializeObject(json) as Dictionary<string, object>;
        }

        private static string FindAuthFile()
        {
            List<string> candidates = new List<string>();
            string codexHome = Environment.GetEnvironmentVariable("CODEX_HOME");
            if (!String.IsNullOrWhiteSpace(codexHome))
                candidates.Add(Path.Combine(codexHome, "auth.json"));

            string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!String.IsNullOrWhiteSpace(profile))
                candidates.Add(Path.Combine(profile, ".codex", "auth.json"));

            candidates.Add(Path.Combine(Environment.CurrentDirectory, ".codex", "auth.json"));
            foreach (string candidate in candidates)
                if (File.Exists(candidate)) return candidate;
            return null;
        }

        private static Dictionary<string, object> GetObject(Dictionary<string, object> source, string key)
        {
            if (source == null || !source.ContainsKey(key)) return null;
            return source[key] as Dictionary<string, object>;
        }

        private static string GetString(Dictionary<string, object> source, string key)
        {
            if (source == null || !source.ContainsKey(key) || source[key] == null) return "";
            return Convert.ToString(source[key], System.Globalization.CultureInfo.InvariantCulture);
        }

        private static int GetInt(Dictionary<string, object> source, string key)
        {
            long value = GetLong(source, key);
            if (value > Int32.MaxValue) return Int32.MaxValue;
            if (value < Int32.MinValue) return Int32.MinValue;
            return (int)value;
        }

        private static long GetLong(Dictionary<string, object> source, string key)
        {
            if (source == null || !source.ContainsKey(key) || source[key] == null) return 0;
            try { return Convert.ToInt64(source[key], System.Globalization.CultureInfo.InvariantCulture); }
            catch { return 0; }
        }
    }
}
