using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Syscom.App.Push.API.Models;

namespace Syscom.App.Push.API.Controllers
{
    [Route("api/v1/[controller]")]
    public class ApiController : Controller
    {
        private const int PAGE_SIZE = 1;
        private readonly AppPushDBContext _dbContext;
        private readonly ILogger _logger;
        public ApiController(AppPushDBContext context,
                             ILogger<AppController> logger)
        {
            _dbContext = context;
            _logger = logger;
        }
        // GET api/v1/api
#warning TODO try catch, log, start date end date
        [HttpGet]
        public JObject Get(
                           [FromQuery] string query = null,
                           [FromQuery] string order = null,
                           [FromQuery] int page = 1)
        {
            IQueryable<Apis> apis = _dbContext.Apis
                                              .Include("Histories")
                                              .Where(q => q.DeletedAt == null);

            if (query != null)
            {
                JObject objQuery = JObject.Parse(query);

                Dictionary<string, string> dictQuery = objQuery.ToObject<Dictionary<string, string>>();
                foreach (string key in dictQuery.Keys)
                {

                    string value = dictQuery[key];

                    switch (key)
                    {
                        case "name":
                            _logger.LogError(key);
                            _logger.LogError(value);
                            apis = apis.Where(q => q.Name.Contains(value));
                            break;
                    }
                }
            }

            if (order != null)
            {
                JObject objOrder = JObject.Parse(order);
                Dictionary<string, string> dictOrder = objOrder.ToObject<Dictionary<string, string>>();
                foreach (string key in dictOrder.Keys)
                {
                    string value = dictOrder[key];
                    switch (key)
                    {
                        case "number":
                            apis = value == "desc" ?
                                    apis.OrderByDescending(q => q.Id) :
                                       apis.OrderBy(q => q.Id);
                            break;
                        case "name":
                            apis = value == "desc" ?
                                    apis.OrderByDescending(q => q.Name) :
                                       apis.OrderBy(q => q.Name);
                            break;
                        case "count":
                            apis = value == "desc" ?
                                apis.OrderByDescending(q => q.Histories.Count) :
                                       apis.OrderBy(q => q.Histories.Count);
                            break;
                        case "status":
                            apis = value == "desc" ?
                                apis.OrderByDescending(q => q.Status) :
                                       apis.OrderBy(q => q.Status);
                            break;
                    }
                }
            }
            else
            {
                apis = apis.OrderByDescending(q => q.CreatedAt);
            }

            var total = apis.Count();

            if (page != 1)
            {
                _logger.LogError("" + page);
                int skip = (page - 1) * PAGE_SIZE;
                apis = apis.Skip(skip);
            }

            var apisData = apis.Select(q => new Apis
            {
                Id = q.Id,
                Name = q.Name,
                Status = q.Status,
                CreatedAt = q.CreatedAt,
                UpdatedAt = q.UpdatedAt,
                PushCount = q.Histories.Count()
            })
            .Take(PAGE_SIZE).ToList();

            JArray data = new JArray();
            foreach (Apis api in apisData)
            {
                dynamic item = new JObject();
                item["id"] = api.Id;
                item["frequency"] = api.PushCount;
                item["name"] = api.Name;
                item["status"] = api.Status;
                item["created_at"] = api.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                item["updated_at"] = api.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                data.Add(item);
            }

            dynamic output = new JObject();

            output["current_page"] = page;
            output["data"] = data;
            output["per_page"] = PAGE_SIZE;
            output["total"] = total;

            dynamic res = new JObject();
            res.code = 200;
            res.reason = "success";
            res.data = output;

            return res;
        }

        [HttpGet("{id}")]
        public JObject Get(int id)
        {
            Apis api = _dbContext.Apis.Find(id);

            dynamic res = new JObject();
            if (api == null)
            {
                res.code = 404;
                res.reason = "api not found";
                return res;
            }
            dynamic item = new JObject();

            item["id"]          = api.Id;
            item["name"]        = api.Name;
            item["app_secret"]  = api.SecretKey;
            item["app_id"]      = api.Uuid;
            item["webhook_url"] = api.Webhook;
            item["status"]      = api.Status;
            item["created_at"]  = api.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
            item["updated_at"]  = api.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss");

            List<Channels> channels = _dbContext.Channels
                                                .OrderBy(q => q.Type)
                                                .ToList();

            List<ApiChannels> apiChannels = _dbContext.ApiChannels.Where(q => q.ApiId == id)
                                                      .Where(q => q.DeletedAt == null)
                                                      .ToList();

            // collect data 
            Dictionary<byte, List<Channels>> collects = new Dictionary<byte, List<Channels>>();

            foreach (Channels channel in channels)
            {
                _logger.LogWarning(channel + "23");
                byte type = (byte)(channel.Type ?? (-1));

                if (type == -1)
                {
#warning LOG
                    continue;
                }
                if (! collects.ContainsKey(type))
                {
                    collects[type] = new List<Channels>();
                }
                collects[type].Add(channel);
            }
            JArray jChannles = new JArray();

            foreach (byte type in collects.Keys)
            {
                List<Channels> channles = collects[type];
                // Email 跟 SMS 只會有一個子項目
                if (type == (byte) Channels.Types.Email ||
                    type == (byte) Channels.Types.SMS)
                {
                    Channels channel = channles.First();
                    string name = type == (byte)Channels.Types.Email ? "E-Mail" : "SMS";

                    dynamic channelData = new JObject();
                    channelData["name"] = name;

                    // set group item
                    dynamic groupItem = new JObject();
                    groupItem["id"] = channel.Id;
                    groupItem["name"] = channel.Name;
                    groupItem["enable"] = apiChannels.Exists(x => x.ChannelId == channel.Id); ;

                    JArray groups = new JArray();
                    groups.Add(groupItem);

                    channelData["groups"] = groupItem;
                    // set gorup in channels
                    jChannles.Add(channelData);
                }

                // app and line
                if (type == (byte)Channels.Types.App ||
                    type == (byte)Channels.Types.Line)
                {
                    string name = type == (byte)Channels.Types.App ? "APP" : "Line";

                    dynamic channelData = new JObject();

                    channelData["name"] = name;

                    JArray groups = new JArray();

                    foreach (Channels channelItem in channles)
                    {
                        dynamic groupItem   = new JObject();

                        bool isEnable = apiChannels.Exists(x => x.ChannelId == channelItem.Id);

                        groupItem["id"]     = channelItem.Id;
                        groupItem["name"]   = channelItem.Name;
                        groupItem["enable"] = isEnable;

                        groups.Add(groupItem);
                    }
                    channelData["groups"] = groups;
                    jChannles.Add(channelData);
                }
               
            }


            dynamic last_update = new JObject();
            last_update["name"] = "";
            last_update["updated_at"] = api.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss");
            item["last_update"] = last_update;

            item["channels"] = jChannles;

            res.code = 200;
            res.reason = "success";
            res.data = item;
            return res;
        }

        // POST api/v1/values/{id}
        [HttpPost("{id?}")]
        public JObject CreateOrUpdate([FromBody]JObject value, int id = 0)
        {
            Apis item = null;

            var name = value.Value<string>("name");
            var enable = value.Value<bool>("status");
            var webhook_url = value.Value<string>("webhook_url");
            JArray channels = (Newtonsoft.Json.Linq.JArray) value.Values<JArray>("channels");

            dynamic res = new JObject();
            res.code = 200;

            JObject output = new JObject();

            if (name == null || name.Trim().Length == 0)
            {

                output["name"] = "名稱不能為空白";

                res.code = 201;
                res.data = output;
            }

            if (res.code != 200)
            {
                //LOG
                return res;
            }

            using (var transaction = _dbContext.Database.BeginTransaction())
            {
                if (id == 0)
                {

                    item = new Apis();
                    item.Name = name;
                    item.Status = (byte)(enable ? Apis.Statuses.Enable : Apis.Statuses.Disable);

                    _dbContext.Apis.Add(item);
                }
                else
                {
                    item = _dbContext.Apis.Find(id);
                    item.Name = name;
                    item.Status = (byte)(enable ? Apis.Statuses.Enable : Apis.Statuses.Disable);
                }
                // save first for the App id
                _dbContext.SaveChanges();

                List<ApiChannels> apiChannels = _dbContext.ApiChannels.Where(q => q.ApiId == item.Id).ToList();

                for (int idx = 0; idx < channels.Count(); idx ++ )
                {
                    JToken c = channels[idx];
                    JArray channelGroups = (JArray) c["groups"];

                    for (int x = 0; x < channelGroups.Count(); x++)
                    {
                        JToken subGroup = channelGroups[x];

                        int channelId = subGroup.Value<int>("id");

                        bool eanble = (bool) subGroup["enable"];

                        ApiChannels api = apiChannels.Find(q => q.Channel.Id == channelId);

                        // client 要啟動 server 沒資料
                        if (eanble && api == null)
                        {
                            _dbContext.Add(new ApiChannels()
                            {
                                ApiId = item.Id,
                                ChannelId = channelId,
                            });
                        }
                        // client 取消 server 有資料
                        if (! eanble && api != null)
                        {
                            _dbContext.Remove(api);
                        }
                    }

                }
                _dbContext.SaveChanges();

                transaction.Commit();
            }

            return res;
        }

        [HttpGet("{id}/history")]
        public JObject History(int id,
                              [FromQuery] string query = null,
                              [FromQuery] string order = null,
                              [FromQuery] int page = 1)
        {

            IQueryable<Histories> histories = _dbContext.Histories
                                                        .Where(q => q.ApiId == id)
                                                        .Include("HistoryDetails");

            if (query != null)
            {
                JObject objQuery = JObject.Parse(query);

                Dictionary<string, string> dictQuery = objQuery.ToObject<Dictionary<string, string>>();
                foreach (string key in dictQuery.Keys)
                {

                    string value = dictQuery[key];

                    switch (key)
                    {
                        case "name":
                            _logger.LogError(key);
                            _logger.LogError(value);
                            histories = histories.Where(q => q.Name.Contains(value));
                            break;
                    }
                }
            }
            if (order != null)
            {
                JObject objOrder = JObject.Parse(order);
                Dictionary<string, string> dictOrder = objOrder.ToObject<Dictionary<string, string>>();
                foreach (string key in dictOrder.Keys)
                {
                    string value = dictOrder[key];
                    switch (key)
                    {
                        case "id":
                            histories = value == "desc" ?
                                    histories.OrderByDescending(q => q.Id) :
                                       histories.OrderBy(q => q.Id);
                            break;
                        case "name":
                            histories = value == "desc" ?
                                    histories.OrderByDescending(q => q.Name) :
                                       histories.OrderBy(q => q.Name);
                            break;
                        case "total":
                            histories = value == "desc" ?
                                histories.OrderByDescending(q => q.Total) :
                                       histories.OrderBy(q => q.Total);
                            break;
                        case "success":
                            histories = value == "desc" ?
                                histories.OrderByDescending(q => q.Success) :
                                       histories.OrderBy(q => q.Success);
                            break;
                        case "fail":
                            histories = value == "desc" ?
                                histories.OrderByDescending(q => q.Fail) :
                                       histories.OrderBy(q => q.Fail);
                            break;
                        case "created_at":
                            histories = value == "desc" ?
                                histories.OrderByDescending(q => q.CreatedAt) :
                                       histories.OrderBy(q => q.CreatedAt);
                            break;
                        case "finished_at":
                            histories = value == "desc" ?
                                histories.OrderByDescending(q => q.FinishedAt) :
                                       histories.OrderBy(q => q.FinishedAt);
                            break;
                    }
                }
            }
            else
            {
                histories = histories.OrderBy(q => q.FinishedAt);
            }

            var total = histories.Count();

            if (page != 1)
            {
                int skip = (page - 1) * PAGE_SIZE;
                histories = histories.Skip(skip);
            }
            _logger.LogWarning(histories.ToString());
            var historiesData = histories.Take(PAGE_SIZE).ToList();

            JArray data = new JArray();
            foreach (Histories history in historiesData)
            {

                dynamic item = new JObject();
                item["id"] = history.Id;
                item["total"] = history.Total;
                item["name"] = history.Name;
                item["success"] = history.Success;
                item["fail"] = history.Fail;
                item["download_url"] = "";
#warning TODO
                item["created_at"] = history.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                item["updated_at"] = history.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                item["finished_at"] = history.FinishedAt?.ToString("yyyy-MM-dd HH:mm:ss");

                data.Add(item);
            }

            dynamic output = new JObject();

            output["current_page"] = page;
            output["data"] = data;
            output["per_page"] = PAGE_SIZE;
            output["total"] = total;

            dynamic res = new JObject();
            res.code = 200;
            res.reason = "success";
            res.data = output;

            return res;

        }
    }
}
