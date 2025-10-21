const { env } = require('process');

// Determine the target based on environment variables or use the correct default ports
const target = 'https://localhost:7294';

console.log(`[Proxy] Using target: ${target}`);

const PROXY_CONFIG = {
  "/api/*": {
    "target": target,
    "secure": false,
    "changeOrigin": false,
    "logLevel": "debug",
    "headers": {
      "Connection": "keep-alive"
    },
    "onProxyReq": (proxyReq, req, res) => {
      // Preserve Authorization header
      if (req.headers.authorization) {
        proxyReq.setHeader('Authorization', req.headers.authorization);
      }
    }
  },
  "/weatherforecast": {
    "target": target,
    "secure": false,
    "changeOrigin": false,
    "logLevel": "debug"
  }
};

module.exports = PROXY_CONFIG;
