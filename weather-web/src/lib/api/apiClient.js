import axios from 'axios';
import { apiBaseUrl } from '../../config/env.js';

export const apiClient = axios.create({
  baseURL: apiBaseUrl,
  timeout: 20000,
  headers: {
    Accept: 'application/json'
  }
});
