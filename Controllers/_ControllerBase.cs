using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kryxivia.AuthLoaderAPI.Controllers
{
    public class _ControllerBase : ControllerBase
    {
        [NonAction]
        public ErrorResult Error()
        {
            return new ErrorResult();
        }

        [NonAction]
        public ErrorObjectResult Error([ActionResultObjectValue] object value)
        {
            return new ErrorObjectResult(value);
        }
    }

    #region Results

    public class ErrorResult : StatusCodeResult
    {
        public ErrorResult() : base(500) { }
    }

    public class ErrorObjectResult : ObjectResult
    {
        public ErrorObjectResult(object value) : base(value)
        {
            StatusCode = 500;
        }
    }

    #endregion
}
