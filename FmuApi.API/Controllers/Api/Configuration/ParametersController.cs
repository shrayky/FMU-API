﻿using FmuApiDomain.Configuration;
using FmuApiDomain.Webix;
using FmuApiSettings;
using JsonSerilizerOptions;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace WebApi.Controllers.Api.Configuration
{
    [Route("api/configuration/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "App configuration")]
    public class ParametersController : Controller
    {
        [HttpGet]
        public IActionResult ParametersGet()
        {
            WebixDataPacket packet = new()
            {
                Content = Constants.Parametrs
            };

            return Ok(packet);
        }

        [HttpPost]
        async public Task<IActionResult> ParametersPostAsync()
        {
            StreamReader? body = new(Request.Body);

            if (body is null)
                return BadRequest("Пустое тело запроса");

            Parametrs? loadPrm;

            try
            {
                loadPrm = await JsonSerializer.DeserializeAsync<Parametrs>(body.BaseStream, GeneralJsonSerilizerOptions.Default());
            }
            catch (Exception ex)
            {
                return BadRequest($"Не удалось преобразовать входящий пакет данных! {ex.Message}");
            }

            if (loadPrm == null)
                return BadRequest();

            Constants.Parametrs = loadPrm;

            await Constants.Parametrs.SaveAsync(Constants.Parametrs, Constants.DataFolderPath);

            return Ok();
        }
    }
}
