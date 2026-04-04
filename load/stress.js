/**
 * Stress: ramp VUs until http_req_failed exceeds STRESS_MAX_ERROR_RATE (then abort).
 *
 * Examples:
 *   k6 run load/stress.js
 *   k6 run -e STRESS_MAX_ERROR_RATE=0.05 -e STRESS_MAX_VUS=300 load/stress.js
 *
 * Env: BASE_URL, STRESS_MAX_ERROR_RATE (default 0.10), STRESS_STAGE_DURATION (default 2m),
 *      STRESS_MAX_VUS (default 500), STRESS_VUS_STEP (default 50), FEED_LIMIT, TEST_*
 */

import { getBaseUrl, envInt, envFloat, getFeedLimit } from './lib/config.js';
import { login, runLoadIteration } from './lib/http.js';

const maxErrRate = envFloat('STRESS_MAX_ERROR_RATE', 0.1);

function buildStages() {
  const stageDur = __ENV.STRESS_STAGE_DURATION || '2m';
  const maxVus = envInt('STRESS_MAX_VUS', 500);
  const step = envInt('STRESS_VUS_STEP', 50);
  const stages = [{ duration: '30s', target: Math.min(10, maxVus) }];
  let current = stages[0].target;
  while (current < maxVus) {
    current = Math.min(current + step, maxVus);
    stages.push({ duration: stageDur, target: current });
  }
  return stages;
}

export const options = {
  scenarios: {
    stress: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: buildStages(),
    },
  },
  thresholds: {
    http_req_failed: [
      {
        threshold: `rate<${maxErrRate}`,
        abortOnFail: true,
      },
    ],
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
