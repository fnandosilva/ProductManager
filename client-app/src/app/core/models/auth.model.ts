export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthResponse {
  token: string;
  username: string;
  email: string;
}

export interface AuthUser {
  username: string;
  email: string;
}
