/**
 * Load: sustained traffic (default 100 VUs, 30 minutes).
 *
 * Without TEST_EMAIL / TEST_PASSWORD / TEST_CREATOR_ID, only GET /health is exercised (CI-friendly).
 * With credentials + TEST_CREATOR_ID, also GET /feed/shuffled/{creatorId}.
 *
 * Examples:
 *   k6 run load/load.js
 *   k6 run -e BASE_URL=http://localhost:7000 -e K6_VUS=50 -e K6_DURATION=10m load/load.js
 *   k6 run -e TEST_EMAIL=... -e TEST_PASSWORD=... -e TEST_CREATOR_ID=<guid> load/load.js
 *
 * Env: BASE_URL, K6_VUS (default 100), K6_DURATION (default 30m), FEED_LIMIT, TEST_*
 */

import { getBaseUrl, envInt, getFeedLimit, getDuration } from './lib/config.js';
import { login, runLoadIteration } from './lib/http.js';

export const options = {
  scenarios: {
    load: {
      executor: 'constant-vus',
      vus: envInt('K6_VUS', 100),
      duration: getDuration('K6_DURATION', '30m'),
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<2000'],
    checks: ['rate>0.95'],
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
