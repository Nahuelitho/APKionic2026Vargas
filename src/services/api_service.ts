import { ajax_request } from './ajax_service';

export type HealthResponse = {
  status: string;
  utc: string;
};

export function probar_api() {
  return ajax_request<HealthResponse>('/health');
}