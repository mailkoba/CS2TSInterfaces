using DemoWebSite.Dto;
using Microsoft.AspNetCore.Mvc;

namespace DemoWebSite.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DemoController : ControllerBase
    {
        [HttpGet("[action]")]
        public ResultDto[] Get()
        {
            return new []
            {
                new ResultDto()
            };
        }

        [HttpPost("[action]")]
        public ResultDto Update([FromBody] RequestDto request)
        {
            return new ResultDto();
        }
    }
}
