﻿using System.Net;

namespace Url_Detection_Agent.Models.API.ServerLicenseAuth
{
    public class ServerLicenseAuthResponse
    {
        public bool Is_valid { get; set; } = false;
        public HttpStatusCode statusCode { get; set; }
    }
}
