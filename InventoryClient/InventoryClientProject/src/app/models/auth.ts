export interface RegisterDto {
  username: string;
  email: string;
  password: string;
  fullName?: string;
  role?: string;
}

export interface LoginDto {
  username: string;
  password: string;
}

export interface AuthResponseDto {
  isSuccess: boolean;
  message: string;
  token?: string;
  expiration?: string;
  username?: string;
  email?: string;
  roles?: string[];
}
