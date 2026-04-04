/**
 * Shared k6 configuration from environment (__ENV).
 * BASE_URL defaults to local gateway (see src/ChatRum.Gateway/Properties/launchSettings.json).
 */

export function getBaseUrl() {
  return __ENV.BASE_URL || 'http://localhost:5136';
}

export function envInt(name, defaultValue) {
  const v = __ENV[name];
  if (v === undefined || v === '') return defaultValue;
  const n = parseInt(v, 10);
  return Number.isFinite(n) ? n : defaultValue;
}

export function envFloat(name, defaultValue) {
  const v = __ENV[name];
  if (v === undefined || v === '') return defaultValue;
  const n = parseFloat(v);
  return Number.isFinite(n) ? n : defaultValue;
}

export function getFeedLimit() {
  return envInt('FEED_LIMIT', 10);
}

export function getDuration(name, defaultValue) {
  const v = __ENV[name];
  return v && v.length > 0 ? v : defaultValue;
}
