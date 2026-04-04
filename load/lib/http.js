import http from 'k6/http';
import { check } from 'k6';

const jsonHeaders = { 'Content-Type': 'application/json' };

/**
 * POST /account/login (via gateway). Returns access_token or null on failure.
 * Response shape: AuthTokenResponse (access_token, token_type, …).
 */
export function login(baseUrl, email, password) {
  const url = `${baseUrl.replace(/\/$/, '')}/account/login`;
  const payload = JSON.stringify({ email, password });
  const res = http.post(url, payload, {
    headers: jsonHeaders,
    tags: { name: 'login' },
  });

  if (res.status !== 200 || !res.body) {
    return null;
  }
  try {
    const body = JSON.parse(res.body);
    return body.access_token || null;
  } catch {
    return null;
  }
}

/**
 * One iteration: GET /health; if token + creatorId, GET /feed/shuffled/{creatorId}?limit=…
 */
export function runLoadIteration(baseUrl, token, creatorId, feedLimit) {
  const root = baseUrl.replace(/\/$/, '');

  const healthRes = http.get(`${root}/health`, {
    tags: { name: 'health' },
  });
  check(healthRes, {
    'health status is 200': (r) => r.status === 200,
  });

  if (token && creatorId) {
    const feedUrl = `${root}/feed/shuffled/${creatorId}?limit=${feedLimit}`;
    const feedRes = http.get(feedUrl, {
      headers: {
        Authorization: `Bearer ${token}`,
      },
      tags: { name: 'feed_shuffled' },
    });
    check(feedRes, {
      'feed_shuffled status is 200': (r) => r.status === 200,
    });
  }
}
