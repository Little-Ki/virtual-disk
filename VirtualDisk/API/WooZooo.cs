using System.Collections.Generic;
using System.Drawing;
using System.Text.Json;
using VirtualDisk.Utils;
using static VirtualDisk.Client.IClient;

namespace VirtualDisk.API
{
    namespace WooZooo
    {
        public class WZFile
        {
            public string Name { get; set; } = string.Empty;
            public string Size { get; set; } = string.Empty;
            public string ID { get; set; } = string.Empty;
        }

        public class WZLink
        {
            public string FileID { get; set; } = string.Empty;
            public string Host { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        public class WZDownload
        {
            public string Dom { get; set; } = string.Empty;
            public string Url { get; set; } = string.Empty;
        }

        public class WZResult<T> where T : class
        {
            public bool Success { get; set; } = false;
            public T? Value { get; set; } = null;
        }

        public class Client
        {
            private readonly Http http = new();

            private string? uid = null;
            public Client()
            {
                http.Header("User-Agent", "Mozilla/5.0");
                http.Header("Accept-Language", "zh-CN");
            }

            public void SetCookie(string value)
            {
                http.Cookie("pc.woozooo.com", value);
                uid = Misc.Match(value, @"ylogin=([0-9]+);", 0, 1);
            }

            public WZResult<List<WZFile>> GetFiles(string folderId, int page, bool folder)
            {
                var result = http.Post(
                    $"https://pc.woozooo.com/doupload.php?uid={uid}",
                    new()
                    {
                        { "task", folder ? "47" : "5" },
                        { "folder_id", folderId },
                        { "uid", uid ?? "" },
                        { "pg", page.ToString() }
                    }
                ).Result;

                var zt = result.Json("zt");
                var success = zt != null && zt.GetValue<int>() == 1;
                var value = new List<WZFile>();

                if (success)
                {
                    var array = result.Json("text")!.AsArray();

                    foreach (var it in array)
                    {
                        value.Add(new WZFile()
                        {
                            Name = it!.AsObject()!["name"]!.GetValue<string>(),
                            Size = folder ? "0" : it!.AsObject()!["size"]!.GetValue<string>(),
                            ID = folder ? it!.AsObject()!["fol_id"]!.GetValue<string>() : it!.AsObject()!["id"]!.GetValue<string>()
                        });
                    }
                }


                return new()
                {
                    Success = success,
                    Value = value
                };
            }

            public WZResult<WZLink> GetSharedLink(string fileId)
            {
                var result = http.Post(
                    $"https://pc.woozooo.com/doupload.php",
                    new()
                    {
                        { "task", "22" },
                        { "file_id", fileId },
                    }
                ).Result;

                var zt = result.Json("zt");
                var success = zt != null && zt.GetValue<int>() == 1;

                return new()
                {
                    Success = success,
                    Value = success ? new()
                    {
                        FileID = result.Json("info", "f_id")!.GetValue<string>(),
                        Host = result.Json("info", "is_newd")!.GetValue<string>(),
                        Password = result.Json("info", "pwd")!.GetValue<string>(),
                    } : new()
                };
            }

            public bool MoveFile(string fileId, string folderId)
            {
                var result = http.Post(
                    $"https://pc.woozooo.com/doupload.php",
                    new()
                    {
                        { "task", "20" },
                        { "file_id", fileId },
                        { "folder_id", folderId },
                    }
                ).Result;

                var zt = result.Json("zt");
                return zt != null && zt.GetValue<int>() == 1;
            }

            public WZResult<string> CreateFolder(string name, string parentId)
            {
                var result = http.Post(
                    $"https://pc.woozooo.com/doupload.php",
                    new()
                    {
                        { "task", "2" },
                        { "parent_id", parentId == "-1" ? "0" : parentId },
                        { "folder_name", name },
                        { "folder_description", "" }
                    }
                ).Result;

                var zt = result.Json("zt");
                var text = result.Json("text")?.GetValue<string>() ?? null;


                return new()
                {
                    Success = zt != null && zt.GetValue<int>() == 1,
                    Value = text
                };
            }

            public bool DeleteFolder(string folderId)
            {
                var result = http.Post(
                    $"https://pc.woozooo.com/doupload.php",
                    new()
                    {
                        { "task", "3" },
                        { "folder_id", folderId }
                    }
                ).Result;

                var zt = result.Json("zt");
                return zt != null && zt.GetValue<int>() == 1;
            }

            public bool DeleteFile(string fileId)
            {
                var result = http.Post(
                    $"https://pc.woozooo.com/doupload.php",
                    new()
                    {
                        { "task", "6" },
                        { "file_id", fileId }
                    }
                ).Result;

                var zt = result.Json("zt");
                return zt != null && zt.GetValue<int>() == 1;
            }

            public byte[] GetData(string url)
            {
                return http.Get(url).Result.Data ?? [];
            }

            public WZResult<string> GetFileUrl(string host, string shareId, string fileId, string pwd)
            {
                var response = http.Get($"{host}/{shareId}", new() { { "Referer", host } }).Result;

                if (!response.Success)
                {
                    return new();
                }

                if (response.Text.Contains("ifr2"))
                {
                    var fn = Misc.Match(response.Text, "src=\"\\/(fn.+?)\"", 0, 1);
                    response = http.Get($"{host}/{fn}", new() { { "Referer", host } }).Result;

                    if (!response.Success)
                    {
                        return new();
                    }

                    var wp_sign = Misc.Match(response.Text, "wp_sign(\\s*)=(\\s*)'(.+?)';", 0, 3);
                    var ajaxdata = Misc.Match(response.Text, "ajaxdata(\\s*)=(\\s*)'(.+?)';", 0, 3);
                    var kdns = Misc.Match(response.Text, "kdns(\\s*)=(\\s*)(\\d+?);", 0, 3);

                    response = http.Post(
                        $"{host}/ajaxm.php?file={fileId}",
                        new()
                        {
                            { "action", "downprocess" },
                            { "websignkey", ajaxdata },
                            { "signs", ajaxdata },
                            { "sign", wp_sign },
                            { "websign", "" },
                            { "kd", "1" },
                            { "ves", "1" }
                        },
                         new() { { "Referer", host } }
                    ).Result;

                    if (!response.Success)
                    {
                        return new();
                    }

                    var zt = response.Json("zt")!.GetValue<int>();
                    var dom = response.Json("dom")!.GetValue<string>();
                    var url = response.Json("url")!.GetValue<string>();

                    return new()
                    {
                        Success = zt == 1,
                        Value = $"{dom}/file/{url}",
                    };
                }

                if (response.Text.Contains("downprocess"))
                {
                    var sign = Misc.Match(response.Text, "'sign':'(.{16,128}?)',", 1, 1);

                    response = http.Post(
                        $"{host}/ajaxm.php?file={fileId}",
                        new()
                        {
                            { "action", "downprocess" },
                            { "sign", sign },
                            { "p", pwd },
                            { "kd", "1" }
                        },
                         new() { { "Referer", host } }
                    ).Result;

                    if (!response.Success)
                    {
                        return new();
                    }

                    var zt = response.Json("zt")!.GetValue<int>();
                    var dom = response.Json("dom")!.GetValue<string>();
                    var url = response.Json("url")!.GetValue<string>();

                    return new()
                    {
                        Success = zt == 1,
                        Value = $"{dom}/file/{url}",
                    };
                }

                return new();
            }
        }
    }
}
