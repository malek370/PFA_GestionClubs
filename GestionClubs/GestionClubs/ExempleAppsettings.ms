# appsetting.Development.json :
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "IdentityProvider": {
    "Authority": "https://localhost:7253",
    "Issuer": "IdentityProvider",
    "Audience": "myappusers"
  }
}
