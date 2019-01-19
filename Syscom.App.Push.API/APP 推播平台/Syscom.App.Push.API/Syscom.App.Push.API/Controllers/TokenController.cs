using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

using Syscom.App.Push.API.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace Syscom.App.Push.API.Controllers
{
    [Route("api/v1/[controller]")]
    public class TokenController : Controller
    {
        private const int PAGE_SIZE = 20;
        private readonly AppPushDBContext _dbContext;
        private readonly ILogger _logger;
        private IConfiguration _config;
        public TokenController(AppPushDBContext context,
                             ILogger<TokenController> logger,
                             IConfiguration config)
        {
            _dbContext = context;
            _logger = logger;
            _config = config;

        }


        // GET api/values/5
        [HttpGet("{id}")]
        public JObject Get(int id)
        {

            dynamic res = new JObject();

            res.code = 200;
            res.reason = "success";
            res.data = id;
            return res;
        }

        // POST api/v1/values/{id}
        [HttpPost("{Login}")]
        public JObject CreateOrUpdate([FromBody]JObject value)
        {
            Tokens tokenItem = null;
            var account = value.Value<string>("account");
            var password = value.Value<string>("password");

            dynamic res = new JObject();
            res.code = 200;

            JObject output = new JObject();

            if (account == null || account.Trim().Length == 0)
            {

                output["account"] = "帳號不能為空白";

                res.code = 201;
                res.data = output;
            }
            if (password == null || password.Trim().Length == 0)
            {
                output["password"] = "密碼不能為空白";

                res.code = 201;
                res.data = output;
            }

            if (res.code != 200)
            {
                //LOG
                return res;
            }

            tokenItem = new Tokens();
            tokenItem.Account = account;
            tokenItem.Token = "XXXXXXX";
            tokenItem.CreatedAt = DateTime.Now;
            tokenItem.TimeOutAt = DateTime.Now.AddHours(1);

            _dbContext.Tokens.Add(tokenItem);

            _dbContext.SaveChanges();
            res.reason = "success";
            res.token = tokenItem.Token;
            res.createAt = tokenItem.CreatedAt;
            res.timeOutAt = tokenItem.TimeOutAt;

            return res;
        }

    }
}
