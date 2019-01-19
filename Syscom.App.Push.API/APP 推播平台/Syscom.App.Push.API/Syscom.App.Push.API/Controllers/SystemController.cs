using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Syscom.App.Push.API.Models;
using Syscom.App.Push.API.Libs;
using Microsoft.EntityFrameworkCore;
using FcmSharp;
using FcmSharp.Requests;
using FcmSharp.Settings;


namespace Syscom.App.Push.API.Controllers
{
    [Route("api/v1")]
    public class SystemController : Controller
    {
        private readonly AppPushDBContext _dbContext;
        private readonly ILogger _logger;
        private readonly string _folder;
        private readonly string _fir_folder;

        private string apiSecretKey = null;
        private Apis api = null;

        public SystemController(IHostingEnvironment env,
                             AppPushDBContext context,
                             ILogger<AppController> logger)
        {
            _dbContext = context;
            _logger = logger;

            // 把上傳目錄設為：wwwroot\Uploads
            _folder = $@"{env.WebRootPath}{Path.DirectorySeparatorChar}Uploads";
            // 把上傳目錄設為：wwwroot\Uploads\FIR
            _fir_folder = $@"{env.WebRootPath}{Path.DirectorySeparatorChar}Uploads{Path.DirectorySeparatorChar}FIR";
        }

        public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            HttpContext.Request.Headers.TryGetValue("Authorization", out var apiSecretKey);

            if (apiSecretKey.Count() > 0)
            {
                this.apiSecretKey = apiSecretKey;

                this.api = _dbContext.Apis.Where(q => q.SecretKey == apiSecretKey).FirstOrDefault();
            }
        }

        // POST api/v1/image/upload
        [HttpPost("image/upload")]
        public async Task<JObject> ImageUpload(IFormFile photo)
        {
            System.IO.Directory.CreateDirectory(_folder);

            dynamic res = new JObject();

            _logger.LogWarning(_folder);
            _logger.LogWarning(photo + "");
            if (photo == null )
            {
                res.code = 404;
                res.reason = "file not found";
                return res;
            }

            string extension = Path.GetExtension(photo.FileName);

            Stream st = photo.OpenReadStream();
            MemoryStream mst = new MemoryStream();
            st.CopyTo(mst);

            string filename = $@"{Helpers.ToMD5Hash(mst.ToArray())}{extension}";

            var path = $@"{_folder}{Path.DirectorySeparatorChar}{filename}";
            using (var stream = new FileStream(path, FileMode.Create))
            {
                await photo.CopyToAsync(stream);
            }

            res.code = 200;
            res.reason = "success";
            res.data = filename;
            return res;
        }

        // POST api/v1/deviceToken
        // Add deviceToken to Subscribers
        [HttpPost("deviceToken")]
        public async Task<JObject> deviceToken([FromBody]JObject input)
        {
            if (this.api == null)
            {
                return new JObject(){
                    {"code", 401},
                    {"reason", "Authorization failed"}
                };
            }

            var channelId = input.Value<int>("channel_id");
            var os = input.Value<string>("os");
            var deviceToken = input.Value<string>("device_token");

            _logger.LogError("deviceToken POST");

            if (0 == _dbContext
                        .ApiChannels
                        .Where(q => q.ApiId == this.api.Id && q.ChannelId == channelId)
                        .Count())
            {
                return new JObject(){
                    {"code", 404},
                    {"reason", "Can't Access Channel"}
                };
            };

            var subscribersType = Subscribers.Types.IOS;
            if (os == "ios")     { subscribersType = Subscribers.Types.IOS; }
            if (os == "android") { subscribersType = Subscribers.Types.Android; }
            if (os == "line")    { subscribersType = Subscribers.Types.Line; }
            var subscriber = _dbContext
                                .Subscribers
                                .Where(q => q.DeletedAt == null &&  q.ChannelId == channelId && q.DeviceToken == deviceToken && q.Type == (byte)subscribersType)
                                .FirstOrDefault();
            
            if (subscriber == null)
            {
                subscriber = new Subscribers();
                subscriber.ChannelId = channelId;
                subscriber.Type = (byte)subscribersType;
                subscriber.DeviceToken = deviceToken;
                subscriber.Status = (byte)Subscribers.Statuses.Enable;
                _dbContext.Subscribers.Add(subscriber);
            }

            subscriber.Status = (byte)Subscribers.Statuses.Enable;

            _dbContext.SaveChanges();
                      
            dynamic output = new JObject();

            dynamic res = new JObject();
            res.code = 200;
            res.reason = "success";
            res.data = output;

            return res;

        }

        // POST api/v1/sendMessage
        [HttpPost("sendMessage")]
        public async Task<JObject> sendMessage([FromBody]JObject input)
        {
            var id = input.Value<string>("id");
            var title = input.Value<string>("title");
            var message = input.Value<string>("message");
            var time = input.Value<string>("time");
            var channels = input.Value<JArray>("channels");

            foreach (var item in channels.Children())
            {
                int channelId = item.Value<int>("id");

                _logger.LogError($"channelId:{channelId}");

                string[] strDeviceTokens = item.Value<JArray>("device_token")?.ToObject<string[]>();

                var channel = _dbContext.Channels.Find(channelId);

                if (channel == null)
                {
                    continue;
                }

                _logger.LogError($"channel:{channel}");


                // APP
                if (channel.Type == 3)
                {
                    var subscriberTypes = new int[] { (int)Subscribers.Types.IOS, (int)Subscribers.Types.Android };

                    var subscribers = strDeviceTokens == null ?
                                    _dbContext.Subscribers.Where(q => q.DeletedAt == null && q.Status == (byte)Subscribers.Statuses.Enable && subscriberTypes.Contains((int)q.Type) && q.ChannelId == channelId).ToList() :
                                    _dbContext.Subscribers.Where(q => q.DeletedAt == null && q.Status == (byte)Subscribers.Statuses.Enable && subscriberTypes.Contains((int)q.Type) && q.ChannelId == channelId && strDeviceTokens.Contains(q.DeviceToken)).ToList(); ;

                    _logger.LogError($"subscribers:{subscribers.Count}");

                    if (subscribers.Count == 0)
                    {
                        continue;
                    }

                    _logger.LogError($"{_fir_folder}{Path.DirectorySeparatorChar}{channel.ChannelKey}");

                    //var settings = FileBasedFcmClientSettings.CreateFromFile($"{_fir_folder}{Path.DirectorySeparatorChar}{channel.ChannelKey}");
                    //var settings = new FcmClientSettings("wildmud-push", "{\"type\": \"service_account\",  \"project_id\": \"wildmud-push\",  \"private_key_id\": \"e37f921cee98f70c23060141e72eeedcbe9e1b4b\",  \"private_key\": \"-----BEGIN PRIVATE KEY-----\nMIIEvQIBADANBgkqhkiG9w0BAQEFAASCBKcwggSjAgEAAoIBAQCjp+HcD59jTpV0\nSK7RtU5tZcKGpwejduRLM83qAFlI1hPON7IPxOn25r2yMEVrr5CTcrkN1i1R8rJn\nO2alxIDODnt8j+TxoSLgUCnjx4Z0PlWjXXTTTzXY1q+3KjDZUEsCp/6cu2vDl36C\ngq87L5PIsc7tkMscMkTJALrlBtTnGxNHLPP5S2025y3x1ShgLk/dFpXaqPjdEe+f\n3YaDwKil8j/nXS6t6CRaEsWFyXsGgV2B2GJBFAZzW2XagPEiAGF5LWDpXuViS328\n94/oqu+50BE4OGZiXmkJAHEgYyEUueTlrMd+8dw4kuadh6NmvY8Feric7ATZNXxF\nMev9Ds7tAgMBAAECggEAUQa8OQeVGwZb0axwvYxeLaS9uIw3KHQjWKZn80zD599y\nA94ob01Hp0Ibtn7WyBeu5Ynd3F9npdSbBqhuzHDrctnRwty9dfKZQWT/MHLne2Mn\nZFBPmJV1rAuzCOU/NUDfOovxcCkNFFRLwxMv7gZCzZFXSeCv5yBuVPRjFCSbQWYm\n5Q9ilRoo0fVAvWiYsEYBZvf0n7xR7Q3kiutKSkCVxzoyU4d6mo+hmZGzlSYYbkp5\n7ytoFe4K6C+ej6+2HT8ZGlC6TOYI3Bx42E78TMGwj2OGukrOsicRc9Lra2D6TFIp\ncb7s7X8VkF4ORjqEPvzye/K+jLUsjs9bpqLEtfITowKBgQDk3sRBrtNbsONWiDDS\n6DgK/EcRERJmzjOl1Zemhmfsbtrx2pXTm7kKDXDpRzwHI0VQzYCdMRgUv+BGCZhS\nwNCsq6GCRFIWv2WQFODnU+RjagJCbRouQC357Wh/meGsckL75bF0usTyRMgrumQ/\nIaaMbOfYmj69S6J5myjDIPWvNwKBgQC3DiMawQ+G6rjbQvTLvl8uNx3n4FCWgUEP\n6r2BHLJ74F41es4+DG88kAX/rZMV1PR7DVn1B2o7LP9WXuWZN1eBwuqXluGpFZRq\npX192Ushp5Zv6uvH3SgN6nMl6BDxj6vvPMjT++FerSefMjvblNNGC6ub8ljx2wcd\ndSLdiA4c+wKBgGhYI9PqV9RK3iraZqARXVOs1t2yEdirFCL8MWqrhn/lvo5bYMmc\nCo3JuPuyDW0XqIeBWazQ8DCtlht4TmkUHU9L5JOWgHJ8ilpZGnx84/hrIWKViUUi\n35M9qNHcH2ZWpbFgdDpK2HW35CcDkKazudH16PH4yLfW3tlgYwIrabebAoGBAJu3\n9PjfXpwAtDwhGyjuyvz/efs0gJlnXrdxkr9wcAyc8sc/ro5t+XplchTrzQF3ZHoB\nA5NDOYUZZCRPGbVatJ/39aP6gABcESMfoD8cR6NbcsfF6cjdQyODW2zVmwRCmZor\n9RMPY8osNlZgXzcNxSQC7Xr9j9g94DGY4Y3eHVNdAoGAVnPc7R1/JSxi0ZgHUGlo\nTPqIvOJaXdHa2t7kxHuZmFk6AYREnQq9tD2VmtvOQHsUAHp4+xFNITv+bfvfBk2f\nuX/8iTnyIXt73qumxh6VEdqroFqhsupirE86fr8eoYyvAxFzsgrk+xo2320dMPCb\nKuyPRjFBhPqOxRhdBxnti+g=\n-----END PRIVATE KEY-----\n\",  \"client_email\": \"firebase-adminsdk-6ov47@wildmud-push.iam.gserviceaccount.com\",  \"client_id\": \"112247404943112944715\",  \"auth_uri\": \"https://accounts.google.com/o/oauth2/auth\",  \"token_uri\": \"https://oauth2.googleapis.com/token\",  \"auth_provider_x509_cert_url\": \"https://www.googleapis.com/oauth2/v1/certs\",  \"client_x509_cert_url\": \"https://www.googleapis.com/robot/v1/metadata/x509/firebase-adminsdk-6ov47%40wildmud-push.iam.gserviceaccount.com\"}");
                    var fcmKeyObject = JObject.Parse(channel.SecretKey);
                    var projectId = fcmKeyObject.Value<string>("project_id");
                    var settings = new FcmClientSettings(projectId, channel.SecretKey);

                    using (var client = new FcmClient(settings))
                    {
                        var notification = new Notification
                        {
                            Title = title,
                            Body = message
                        };

                        foreach (var subscriber in subscribers)
                        {
                            try
                            {
                                // The Message should be sent to the News Topic:
                                var msg = new FcmMessage()
                                {
                                    ValidateOnly = false,
                                    Message = new Message
                                    {
                                        //Topic = "news",
                                        Token = subscriber.DeviceToken,
                                        Notification = notification
                                    }
                                };

                                // Finally send the Message and wait for the Result:
                                CancellationTokenSource cts = new CancellationTokenSource();

                                // Send the Message and wait synchronously:
                                var result = client.SendAsync(msg, cts.Token).GetAwaiter().GetResult();

                                _logger.LogInformation($"Data Message ID = {result.Name}");
                            }
                            catch (Exception ex)
                            {
                                _logger.LogInformation($"Exception: {ex.ToString()}");
                                continue;
                            }
                        }

                        //return new JObject(){
                        //    { "code", 200},
                        //    { "reason", "scuesss" },
                        //    { "data", new JObject(){{ "message_id", result.Name }} }
                        //};

                    }

                }

                // Line
                if (channel.Type == 4)
                {
                    var subscriberTypes = new int[] { (int)Subscribers.Types.Line };

                    var subscribers = strDeviceTokens == null ?
                                        _dbContext.Subscribers.Where(q => q.DeletedAt == null && q.Status == (byte)Subscribers.Statuses.Enable && subscriberTypes.Contains((int)q.Type) && q.ChannelId == channelId).ToList() :
                                        _dbContext.Subscribers.Where(q => q.DeletedAt == null && q.Status == (byte)Subscribers.Statuses.Enable && subscriberTypes.Contains((int)q.Type) && q.ChannelId == channelId && strDeviceTokens.Contains(q.DeviceToken)).ToList(); ;

                    _logger.LogError($"subscribers:{subscribers.Count}");

                    if (subscribers.Count == 0)
                    {
                        continue;
                    }

                    foreach (var subscriber in subscribers)
                    {
                        
                        try
                        {

                            var client = new HttpClient();
                            
                            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {channel.SecretKey}");

                            var data = new
                            {
                                to = subscriber.DeviceToken,
                                messages = new[] {
                                        new {
                                            type = "text",
                                            text = message,
                                        }
                                   }
                            };

                            var json = JsonConvert.SerializeObject(data);

                            var content = new StringContent(json, Encoding.UTF8, "application/json");

                            content.Headers.ContentLength = json.Length;

                            var result = await client.PostAsync("https://api.line.me/v2/bot/message/push", content);

                            _logger.LogError($"result.Content.ToString(): {result}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Exception: {ex.ToString()}");
                            continue;
                        }
                    }

                }
            }

            dynamic output = new JObject();

            output["message_id"] = 3345678;

            dynamic res = new JObject();
            res.code = 200;
            res.reason = "success";
            res.data = output;

            return res;

        }

        // POST api/v1/sendMessage Content-Type
        [HttpPost("sendMessageByFile")]
        public async Task<JObject> sendMessageByFile(IFormFile file)
        {
            if (file == null)
            {
                return new JObject(){
                    {"code", 404},
                    {"reason", "file not found"},
                    {"data", new JObject() }
                };
            }

            using (StreamReader reader = new StreamReader(file.OpenReadStream()))
            {
                
                string line = " ";
                
                Histories history = null;
                while (line != null)
                {
                    try
                    {
                        line = reader.ReadLine();

                        if (line == null) { continue; }

                        _logger.LogInformation($"AAA, {line}");

                        _logger.LogInformation($"BBB, { line.Length}");

                        string pattern = @"""\s*,\s*""";

                        string[] item = line.IndexOf('"') >= 0 ?
                                            System.Text.RegularExpressions.Regex.Split(line.Substring(1, line.Length - 2), pattern)
                                            : line.Split(',');

                        _logger.LogInformation($"item.Count(): \n {item.Count()}");

                        if (item == null || item.Count() < 4) { continue; }

                        _logger.LogInformation($"item.Count() != 0: \n {item.ToString()}", item);

                        var apiId = Int32.Parse(item[0]);
                        var deviceToken = item[1];
                        var pushMessage = item[2];
                        var channelId = Int32.Parse(item[3]);

                        _logger.LogInformation($"CCCC, {apiId}:{deviceToken}:{pushMessage}:{channelId}");

                        if (history == null)
                        {

                            history = _dbContext.Add<Histories>(new Histories()
                            {
                                ApiId = apiId,
                                Total = 100,
                                Success = 0,
                                Fail = 0,
                                Name = "上傳",
                                CreatedAt = DateTime.Now,
                                UpdatedAt = DateTime.Now
                            }).Entity;

                        }



                        var subscriber = _dbContext.Subscribers
                                                   .Where(q => q.DeletedAt == null)
                                                   .Where(q => q.ChannelId == channelId)
                                                   .Where(q => q.DeviceToken == deviceToken)
                                                   .First();

                        if (subscriber == null) { continue; }

                        _logger.LogInformation($"DDDD, {subscriber}");

                        var historyDetail = _dbContext.Add(new HistoryDetails()
                        {
                            HistoriesId = history.Id,
                            SubscriberId = subscriber.Id,
                            ChannelId = channelId,
                            Message = pushMessage,
                            Status = 1,
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        }).Entity;

                    }
                    catch (Exception ex)
                    {
                        _logger.LogInformation($"Exception: {ex.ToString()}");
                        continue;
                    }

                }
                _dbContext.SaveChanges();
                reader.Dispose();
            }

            var filename = "1234";
            return new JObject(){
                {"code", 200},
                {"reason", "success"},
                {"data", filename}
            };
        }
    }
}
