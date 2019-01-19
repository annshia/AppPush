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
    public class ChannelController : Controller
    {
        private const int PAGE_SIZE = 10;
        private readonly AppPushDBContext _dbContext;
        private readonly ILogger _logger;

        private string apiSecretKey = null;
        private Apis api = null;
        public ChannelController(AppPushDBContext context,
                             ILogger<AppController> logger)
        {
            _dbContext = context;
            _logger = logger;
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

        // GET api/v1/Channel
        public JObject Get([FromQuery] string query = null,
                           [FromQuery] string order = null,
                           [FromQuery] int page = 1)
        {
            _logger.LogInformation("ChannelController GET");

            if (this.api == null)
            {
                return new JObject(){
                    { "code", 404},
                    { "reason", "can't find api" },
                    { "data", new JObject() }
                };
            }

            var channelIds = _dbContext.ApiChannels.Where(ac => ac.ApiId == this.api.Id).Select(ac => ac.ChannelId);

            IQueryable<Channels> channels = _dbContext.Channels
                                                      .Where(c => channelIds.Contains(c.Id));

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
                        case "id":
                            channels = value == "desc" ?
                                channels.OrderByDescending(q => q.Id) :
                                channels.OrderBy(q => q.Id);
                            break;
                        case "name":
                            channels = value == "desc" ?
                                channels.OrderByDescending(q => q.Name) :
                                channels.OrderBy(q => q.Name);
                            break;
                        case "type":
                            channels = value == "desc" ?
                                channels.OrderByDescending(q => q.Type) :
                                channels.OrderBy(q => q.Type);
                            break;
                    }
                }
            }
            else
            {
                channels = channels.OrderBy(q => q.CreatedAt);
            }

            var total = channels.Count();

            if (page != 1)
            {
                int skip = (page - 1) * PAGE_SIZE;
                channels = channels.Skip(skip);
            }
            _logger.LogWarning(channels.ToString());
            var channelsData= channels.Take(PAGE_SIZE).ToList();

            JArray data = new JArray();
            foreach (Channels channel in channelsData)
            {

                dynamic item = new JObject();
                item["id"] = channel.Id;
                item["type"] = channel.Type;
                item["name"] = channel.Name;

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
