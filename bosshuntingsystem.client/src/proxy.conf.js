const { env } = require('process');

// Determine the target based on environment variables or use the correct default ports
const target = env.ASPNETCORE_HTTP_PORT ? `http://localhost:${env.ASPNETCORE_HTTP_PORT}` :
               env.ASPNETCORE_HTTPS_PORT ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}` :
               'http://localhost:5077'; // Updated to match actual running port

console.log(`[Proxy] Using target: ${target}`);

const PROXY_CONFIG = {
  "/api/*": {
    "target": target,
    "secure": false,
    "changeOrigin": true,
    "logLevel": "debug"
  },
  "/weatherforecast": {
    "target": target,
    "secure": false,
    "changeOrigin": true,
    "logLevel": "debug"
  }
};

module.exports = PROXY_CONFIG;
