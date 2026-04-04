/**
 * Soak: steady load for a long time (default 12h) to surface leaks or degradation.
 *
 * k6 only reports HTTP metrics from the load generator. To watch .NET memory/GC on the server,
 * use dotnet-counters, Prometheus/Grafana, or your host metrics alongside this run.
 *
 * JWT access tokens expire; for multi-hour runs, omit TEST_CREATOR_ID to stress /health only, or
 * rely on your token lifetime / refresh strategy.
 *
 * Examples:
 *   k6 run load/soak.js
 *   k6 run -e SOAK_DURATION=30m -e SOAK_VUS=5 load/soak.js
 *
 * Env: BASE_URL, SOAK_VUS (default 15), SOAK_DURATION (default 12h), FEED_LIMIT, TEST_*
 */

import { getBaseUrl, envInt, getFeedLimit, getDuration } from './lib/config.js';
import { login, runLoadIteration } from './lib/http.js';

export const options = {
  scenarios: {
    soak: {
      executor: 'constant-vus',
      vus: envInt('SOAK_VUS', 15),
      duration: getDuration('SOAK_DURATION', '12h'),
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.05'],
    http_req_duration: ['p(95)<3000'],
  },
};

export function setup() {
  const baseUrl = getBaseUrl();
  let token = null;
  if (__ENV.TEST_EMAIL && __ENV.TEST_PASSWORD) {
    token = login(baseUrl, __ENV.TEST_EMAIL, __ENV.TEST_PASSWORD);
  }
  return {
    baseUrl,
    token,
    creatorId: __ENV.TEST_CREATOR_ID || null,
    feedLimit: getFeedLimit(),
  };
}

export default function (data) {
  runLoadIteration(data.baseUrl, data.token, data.creatorId, data.feedLimit);
}
