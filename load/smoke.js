/**
 * Smoke: minimal VUs, short duration — verifies k6 scripts and that the API is up.
 *
 * Examples:
 *   k6 run load/smoke.js
 *   k6 run -e BASE_URL=http://localhost:7000 load/smoke.js
 *   k6 run -e TEST_EMAIL=user@example.com -e TEST_PASSWORD=secret -e TEST_CREATOR_ID=<guid> load/smoke.js
 *
 * Env: BASE_URL, SMOKE_VUS (1–5 typical), SMOKE_DURATION, FEED_LIMIT,
 *      TEST_EMAIL, TEST_PASSWORD, TEST_CREATOR_ID (optional; feed GET needs all three for auth path)
 */

import { getBaseUrl, envInt, getFeedLimit } from './lib/config.js';
import { login, runLoadIteration } from './lib/http.js';

export const options = {
  scenarios: {
    smoke: {
      executor: 'constant-vus',
      vus: Math.min(5, Math.max(1, envInt('SMOKE_VUS', 3))),
      duration: __ENV.SMOKE_DURATION || '90s',
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.01'],
    checks: ['rate>0.99'],
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
