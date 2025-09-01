const { env } = require('process');

// Determine the target based on environment variables or use the default HTTP port
let target;
if (env.ASPNETCORE_HTTP_PORT) {
  target = `http://localhost:${env.ASPNETCORE_HTTP_PORT}`;
} else if (env.ASPNETCORE_HTTPS_PORT) {
  target = `https://localhost:${env.ASPNETCORE_HTTPS_PORT}`;
} else if (env.ASPNETCORE_URLS) {
  // Use the first URL from the list, preferring HTTP for development
  const urls = env.ASPNETCORE_URLS.split(';');
  target = urls.find(url => url.startsWith('http://')) || urls[0];
} else {
  // Default to HTTP port for development
  target = 'http://localhost:5077';
}

console.log(`[Proxy] Using target: ${target}`);

const PROXY_CONFIG = [
  {
    context: [
      "/weatherforecast",
      "/api",
      "/api/**"
    ],
    target,
    secure: false,
    changeOrigin: true,
    logLevel: "debug",
    headers: {
      "Connection": "keep-alive"
    },
    onProxyReq: (proxyReq, req, res) => {
      console.log(`[Proxy] Proxying ${req.method} ${req.url} to ${target}`);
    },
    onError: (err, req, res) => {
      console.error(`[Proxy] Error proxying ${req.method} ${req.url}:`, err.message);
    }
  }
]

module.exports = PROXY_CONFIG;
