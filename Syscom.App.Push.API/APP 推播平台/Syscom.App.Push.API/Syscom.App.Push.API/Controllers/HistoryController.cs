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
    public class HistoryController : Controller
    {
        private const int PAGE_SIZE = 1;
        private readonly AppPushDBContext _dbContext;
        private readonly ILogger _logger;
        public HistoryController(AppPushDBContext context,
                             ILogger<AppController> logger)
        {
            _dbContext = context;
            _logger = logger;
        }

        public JObject Get([FromQuery] string query = null,
                           [FromQuery] string order = null,
                           [FromQuery] int page = 1)
        {

            IQueryable<Histories> histories = _dbContext.Histories
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
                        case "success" :
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

        // GET api/values/5
        [HttpGet("{id}")]
        public JObject Get(int id,
                          [FromQuery] string query = null,
                          [FromQuery] string order = null,
                          [FromQuery] int page = 1)
        {
            IQueryable<HistoryDetails> histories = _dbContext.HistoryDetails
                                                             .Include(p => p.Subscriber)
                                                             .Include(p => p.Channel)
                                                             .Where(q => q.HistoriesId == id);

            if (query != null)
            {
                JObject objQuery = JObject.Parse(query);

                Dictionary<string, string> dictQuery = objQuery.ToObject<Dictionary<string, string>>();
                foreach (string key in dictQuery.Keys)
                {

                    string value = dictQuery[key];

                    switch (key)
                    {
                        case "subscriber_id":
                            _logger.LogError(key);
                            _logger.LogError(value);
                            histories = histories.Where(q => q.Subscriber.DeviceToken.Contains(value));
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
                        //case "id":
                        //    histories = value == "desc" ?
                        //            histories.OrderByDescending(q => q.Id) :
                        //               histories.OrderBy(q => q.Id);
                        //    break;
                        //case "name":
                        //    histories = value == "desc" ?
                        //            histories.OrderByDescending(q => q.Name) :
                        //               histories.OrderBy(q => q.Name);
                        //    break;
                        //case "total":
                        //    histories = value == "desc" ?
                        //        histories.OrderByDescending(q => q.Total) :
                        //               histories.OrderBy(q => q.Total);
                        //    break;
                        //case "success":
                        //    histories = value == "desc" ?
                        //        histories.OrderByDescending(q => q.Success) :
                        //               histories.OrderBy(q => q.Success);
                        //    break;
                        //case "fail":
                        //    histories = value == "desc" ?
                        //        histories.OrderByDescending(q => q.Fail) :
                        //               histories.OrderBy(q => q.Fail);
                        //    break;
                        //case "created_at":
                        //    histories = value == "desc" ?
                        //        histories.OrderByDescending(q => q.CreatedAt) :
                        //               histories.OrderBy(q => q.CreatedAt);
                        //    break;
                        //case "finished_at":
                            //histories = value == "desc" ?
                            //    histories.OrderByDescending(q => q.FinishedAt) :
                            //           histories.OrderBy(q => q.FinishedAt);
                            //break;
                    }
                }
            }
            else
            {
                histories = histories.OrderBy(q => q.CreatedAt);
            }

            var total = histories.Count();

            if (page != 1)
            {
                int skip = (page - 1) * PAGE_SIZE;
                histories = histories.Skip(skip);
            }

            var historiesData = histories.Take(PAGE_SIZE).ToList();

            JArray data = new JArray();
            foreach (HistoryDetails history in historiesData)
            {
                string platform = "";

                if (history.Channel.Type == (byte) Channels.Types.App)
                {
                    platform = history.Subscriber.Type == (byte) Subscribers.Types.Android ? 
                                                        "Android" : 
                                                      "iOS";
                }
                if (history.Channel.Type == (byte)Channels.Types.Line)
                {
                    platform = "Line";
                }
                dynamic item = new JObject();
                item["id"] = history.Id;
                item["subscriber_id"] = history.Subscriber.DeviceToken;
                item["platform"] = platform;
                item["channel"] = history.Channel.Name;
                item["message"] = history.Message;
                item["status"] = history.Status;
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
