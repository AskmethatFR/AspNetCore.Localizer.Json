﻿using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace AspNetCore.Localizer.Json.Test.Helpers
{
    public class HostingEnvironmentStub : IWebHostEnvironment
    {
        public HostingEnvironmentStub()
        {
        }

        public string EnvironmentName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string ApplicationName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string WebRootPath { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IFileProvider WebRootFileProvider { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string ContentRootPath { get => AppContext.BaseDirectory; set => throw new NotImplementedException(); }
        public IFileProvider ContentRootFileProvider { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }
}
