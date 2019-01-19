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
    public class LineController : Controller
    {
        private const int PAGE_SIZE = 1;
        private readonly AppPushDBContext _dbContext;
        private readonly ILogger _logger;
        public LineController(AppPushDBContext context,
                             ILogger<AppController> logger)
        {
            _dbContext = context;
            _logger = logger;
        }
        // GET api/v1/app
#warning TODO try catch, log, code define
        [HttpGet]
        public JObject Get([FromQuery] string query = null,
                           [FromQuery] string order = null,
                           [FromQuery] int page = 1)
        {
            IQueryable<Channels> channels = _dbContext.Channels
                                                    .Include("Subscribers")
                                                     .Where(q => q.Type == (byte) Channels.Types.Line)
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
                            channels = channels.Where(q => q.Name.Contains(value));
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
                            channels = value == "desc" ?
                                    channels.OrderByDescending(q => q.Id) :
                                       channels.OrderBy(q => q.Id);
                            break;
                        case "name":
                            channels = value == "desc" ?
                                    channels.OrderByDescending(q => q.Name) :
                                       channels.OrderBy(q => q.Name);
                            break;
                        case "members":
                            channels = value == "desc" ?
                                channels.OrderByDescending(q => q.Subscribers.Count) :
                                       channels.OrderBy(q => q.Subscribers.Count);
                            break;
                        case "push":
                        case "status":
                            channels = value == "desc" ?
                                channels.OrderByDescending(q => q.Status) :
                                       channels.OrderBy(q => q.Status);
                            break;
                    }
                }
            }
            else
            {
                channels = channels.OrderByDescending(q => q.CreatedAt);
            }

            var total = channels.Count();

            if (page != 1)
            {
                _logger.LogError("" + page);
                int skip = (page - 1) * PAGE_SIZE;
                channels = channels.Skip(skip);
            }

            var channelsData = channels.Select(q => new Channels
            {
                Id = q.Id,
                Name = q.Name,
                Type = (byte)Channels.Types.Line,
                Status = q.Status,
                CreatedAt = q.CreatedAt,
                UpdatedAt = q.UpdatedAt,
                Image = q.Image,
                SubscriberCount = q.Subscribers.Count()
            })
                                    .Take(PAGE_SIZE).ToList();

            JArray data = new JArray();
            foreach (Channels channelItem in channelsData)
            {
                dynamic item = new JObject();
                item["id"] = channelItem.Id;
                item["members"] = channelItem.SubscriberCount;
                item["icon"] = channelItem.Image;
                item["name"] = channelItem.Name;
                item["available"] = true;
                item["status"] = channelItem.Status;
                item["created_at"] = channelItem.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                item["updated_at"] = channelItem.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss");


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


        // GET api/values/5
        [HttpGet("{id}")]
        public JObject Get(int id)
        {
            Channels channel = _dbContext.Channels.Find(id);

            dynamic res = new JObject();
            if (channel == null)
            {
                res.code = 404;
                res.reason = "channel not found";
                return res;
            }
            dynamic item = new JObject();
            item["id"] = channel.Id;
            item["members"] = channel.SubscriberCount;
            item["icon"] = channel.Image;
            item["name"] = channel.Name;
            item["line_secret"] = channel.SecretKey;
            item["line_channel_id"] = channel.ForeignId;
            item["webhook"] = channel.ForeignWebhook;
            item["available"] = true;
            item["available_message"] = true;
            item["status"] = channel.Status;
            item["created_at"] = channel.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
            item["updated_at"] = channel.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss");

            dynamic last_update = new JObject();
            last_update["name"] =
            last_update["updated_at"] = channel.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss");
            item["last_update"] = last_update;

            res.code = 200;
            res.reason = "success";
            res.data = item;
            return res;
        }

        // POST api/v1/values/{id}
        [HttpPost("{id?}")]
        public JObject CreateOrUpdate([FromBody]JObject value, int id = 0)
        {
            Channels channelItem = null;
            var name = value.Value<string>("name");
            var icon = value.Value<string>("icon");
            var line_secret = value.Value<string>("secret_key");
            var line_channel_id = value.Value<string>("channel_id");
            var enable = value.Value<bool>("status");

            dynamic res = new JObject();
            res.code = 200;

            JObject output = new JObject();

            if (name == null || name.Trim().Length == 0)
            {
                output["name"] = "名稱不能為空白";

                res.code = 201;
                res.data = output;
            }
            if (line_secret == null || line_secret.Trim().Length == 0)
            {
                output["line_secret"] = "請輸入Line Secret";

                res.code = 201;
                res.data = output;
            }

            if (res.code != 200)
            {
                //LOG
                return res;
            }

            Channels channel = _dbContext.Channels.Where(q => q.Type == (byte)Channels.Types.App).First();

            if (id == 0)
            {

                channelItem = new Channels();
                channelItem.Name = name;
                channelItem.Image = icon ?? null;
                channelItem.ForeignId = line_channel_id ?? null;
                channelItem.SecretKey = line_secret ?? null;
                channelItem.ChannelId = channel.Id;
                channelItem.Status = (byte)(enable ? Channels.Statuses.Enable : Channels.Statuses.Disable);
                _dbContext.Channels.Add(channelItem);
            }
            else
            {
                channelItem = _dbContext.Channels.Find(id);
                channelItem.Name = name;
                channelItem.Image = icon ?? null;
                channelItem.ForeignId = line_channel_id ?? null;
                channelItem.SecretKey = line_secret ?? null;
                channelItem.ChannelId = channel.Id;
                channelItem.Status = (byte)(enable ? Channels.Statuses.Enable : Channels.Statuses.Disable);

            }
            _dbContext.SaveChanges();


            return res;
        }

        [HttpDelete("{id}/subscriber/{sid}")]
        public JObject DeleteSubscribers(int id, int sid)
        {
            Subscribers subscriber = _dbContext.Subscribers.Find(id);

            dynamic res = new JObject();

            if (subscriber != null)
            {
                subscriber.DeletedAt = DateTime.Now;
                _dbContext.SaveChanges();

                res.code = 200;
                res.reason = "success";
            }

            res.data = null;
            return res;
        }
        [HttpGet("{id}/subscriber")]
        public JObject GetSubscribers(int id,
                                      [FromQuery] string querys = null,
                                      [FromQuery] string order = null,
                                      [FromQuery] string start_date = null,
                                      [FromQuery] string end_date = null,
                                      [FromQuery] int page = 1)
        {
            IQueryable<Subscribers> subscribers = _dbContext.Subscribers
                                                            .Where(q => q.Channel.Id == id)
                                                            .Where(q => q.DeletedAt == null);

            if (querys != null)
            {
                JObject objQuery = JObject.Parse(querys);

                Dictionary<string, string> dictQuery = objQuery.ToObject<Dictionary<string, string>>();
                foreach (string key in dictQuery.Keys)
                {

                    string value = dictQuery[key];

                    switch (key)
                    {
                        case "register_id":
                            subscribers = subscribers.Where(q => q.DeviceToken.Contains(value));
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
                            subscribers = value == "desc" ?
                                    subscribers.OrderByDescending(q => q.Id) :
                                       subscribers.OrderBy(q => q.Id);
                            break;
                        case "register_id":
                            subscribers = value == "desc" ?
                                subscribers.OrderByDescending(q => q.DeviceToken) :
                                       subscribers.OrderBy(q => q.DeviceToken);
                            break;
                        case "members":
                            subscribers = value == "os" ?
                                subscribers.OrderByDescending(q => q.Type) :
                                       subscribers.OrderBy(q => q.Type);
                            break;
                        case "updated_at":
                            subscribers = value == "os" ?
                                subscribers.OrderByDescending(q => q.UpdatedAt) :
                                    subscribers.OrderBy(q => q.UpdatedAt);
                            break;
                        case "status":
                            subscribers = value == "desc" ?
                                subscribers.OrderByDescending(q => q.Status) :
                                       subscribers.OrderBy(q => q.Status);
                            break;
                    }
                }
            }
            if (start_date != null && end_date != null)
            {
                DateTime start_datetime = Convert.ToDateTime(start_date);
                DateTime end_datetime = Convert.ToDateTime(end_date);
                start_datetime = new DateTime(
                    start_datetime.Year, start_datetime.Month, start_datetime.Day,
                    0, 0, 0
                );
                end_datetime = new DateTime(
                    end_datetime.Year, end_datetime.Month, end_datetime.Day,
                    23, 59, 59
                );

                subscribers = subscribers.Where(q => q.CreatedAt >= start_datetime &&
                                                     q.CreatedAt <= end_datetime);
            }

            var total = subscribers.Count();

            if (page != 1)
            {
                _logger.LogError("" + page);
                int skip = (page - 1) * PAGE_SIZE;
                subscribers = subscribers.Skip(skip);
            }

            subscribers = subscribers.OrderByDescending(q => q.CreatedAt);

            var output_subscribers = subscribers.Take(PAGE_SIZE).ToList();

            JArray data = new JArray();
            foreach (Subscribers subscriber in output_subscribers)
            {
                dynamic item = new JObject();
                item["id"] = subscriber.Id;
                item["register_id"] = subscriber.DeviceToken;
                item["os"] = subscriber.Type == (byte)Subscribers.Types.Android ? "android" : "ios";
                item["status"] = subscriber.Status;
                item["created_at"] = subscriber.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                item["updated_at"] = subscriber.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss");

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
