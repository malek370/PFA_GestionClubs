# Logging Configuration Guide

This guide explains the comprehensive logging that has been added to the IdentityProvider application.

## Overview

The application now includes detailed logging for:
- **Request/Response logging** - All HTTP requests and responses
- **Token validation logging** - JWT token validation events
- **Authentication events** - Login, registration, and refresh token operations
- **Token generation** - JWT and refresh token creation

## Configuration

### appsettings.Development.json
For development environment with detailed debugging:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.AspNetCore.Authentication": "Debug",
      "Microsoft.AspNetCore.Authorization": "Debug",
      "IdentityProvider": "Debug",
      "IdentityProvider.Processors": "Debug",
      "IdentityProvider.Middleware": "Debug"
    }
  }
}
```

### appsettings.json
For production environment with balanced logging:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.AspNetCore.Authentication": "Information",
      "Microsoft.AspNetCore.Authorization": "Information",
      "IdentityProvider": "Information",
      "IdentityProvider.Processors": "Information",
      "IdentityProvider.Middleware": "Information"
    }
  }
}
```

## What Gets Logged

### Request/Response Logging (RequestResponseLoggingMiddleware)
- **Request Information**: Method, Path, Query String, Remote IP, Headers
- **Response Information**: Status Code, Response Time, Headers
- **Request/Response Body**: For non-binary content types
- **Sensitive Data**: Authorization headers and cookies are excluded from logging

### Authentication & Token Validation
- **JWT Token Receipt**: When tokens are received from cookies
- **Token Validation Success**: User ID, email, and roles from validated tokens
- **Authentication Failures**: Token validation errors, forbidden access
- **Authentication Challenges**: Missing or invalid tokens

### Token Generation (AuthTokenProcessor)
- **Token Creation**: User information and token expiration
- **Refresh Token Generation**: Creation and expiration times
- **Cookie Setting**: Token cookie creation with expiration info
- **Claims Preparation**: Number of roles and user details added to tokens

### Account Service Operations
- **Login Attempts**: Email being used for login
- **Login Success/Failure**: User authentication results with user IDs
- **Registration Attempts**: New user creation attempts
- **Registration Results**: Success/failure with detailed error information
- **Refresh Token Operations**: Token refresh attempts and results

## Log Levels Used

- **Debug**: Detailed technical information (token claims, request/response bodies)
- **Information**: Important application events (successful operations, user actions)
- **Warning**: Potential issues (authentication failures, invalid tokens)
- **Error**: Serious problems (registration failures, user update errors)

## Sample Log Output

### Successful Login
```
[14:30:15 INF] Login attempt for email: user@example.com
[14:30:15 INF] Login successful for user 123e4567-e89b-12d3-a456-426614174000 (user@example.com)
[14:30:15 DBG] Generating and storing tokens for user 123e4567-e89b-12d3-a456-426614174000 (user@example.com)
[14:30:15 INF] Generating asymmetric JWT token for user 123e4567-e89b-12d3-a456-426614174000 (user@example.com)
[14:30:15 DBG] RSA private key loaded successfully for token generation
```

### Token Validation
```
[14:30:16 DBG] JWT token received from ACCESS_TOKEN cookie for request /api/account/protected
[14:30:16 INF] JWT token validated successfully for user 123e4567-e89b-12d3-a456-426614174000 (user@example.com) with roles: Visitor
```

### Request/Response
```
[14:30:15 INF] [a1b2c3d4] Incoming Request: POST /api/account/login from 192.168.1.100
[14:30:15 INF] [a1b2c3d4] Response: 200 in 145ms
```

## Security Considerations

- **Sensitive Headers**: Authorization, Cookie, and API key headers are excluded from logs
- **Request Bodies**: Only logged for non-form data (JSON, XML, text)
- **Token Values**: Actual token values are never logged, only metadata
- **User Passwords**: Never logged in any form
- **Production Logging**: Use Information level or higher to avoid performance impact

## Performance Impact

- **Development**: Debug logging may impact performance due to detailed logging
- **Production**: Information level logging has minimal performance impact
- **Request/Response Body Logging**: Only enabled for specific content types to minimize overhead