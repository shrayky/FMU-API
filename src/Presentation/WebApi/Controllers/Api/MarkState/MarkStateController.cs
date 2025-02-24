﻿using FmuApiApplication.Services.MarkServices;
using FmuApiDomain.MarkInformation.Models;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Api.MarkState
{
    [Route("api/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "Mark state API")]
    public class MarkStateController : ControllerBase
    {
        private readonly ILogger<MarkStateController> _logger;
        private MarkStateSrv _markStateSrv;

        public MarkStateController(ILogger<MarkStateController> logger, MarkStateSrv markStateSrv)
        {
            _logger = logger;
            _markStateSrv = markStateSrv;
        }

        [HttpGet]
        public async Task<IActionResult> StateMark(string mark)
        {
            var info = await _markStateSrv.State(mark);

            return Ok(info);
        }

        [HttpPost]
        public async Task<IActionResult> SaleMark(CheckWithMarks saleMark)
        {
            try
            {
                if (saleMark.CheckData.IsSale)
                    await _markStateSrv.SetMarksSold(saleMark);
                else
                    await _markStateSrv.SetMarksInStok(saleMark);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[{Date}] - Ошибка изменения статусов марок в БД. \r\n {err}", DateTime.Now, ex.Message);
                return BadRequest(ex.Message);
            }

            return Ok();
        }

    }
}
