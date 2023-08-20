using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Text;

namespace AspNetCore.Localizer.Json.JsonOptions
{
    public class EnvironmentWrapper
    {
        private IWebHostEnvironment serverEnvironment;

        public EnvironmentWrapper(IWebHostEnvironment hostingEnvironmentStub)
        {
            this.serverEnvironment = hostingEnvironmentStub;
        }

        public string ContentRootPath { get; internal set; }
        public bool IsWasm { get; } = false;
    }
}