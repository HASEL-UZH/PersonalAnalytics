import electron from 'electron';

export declare interface Is {
  dev: boolean;
}

export const is: Is = {
  dev: !electron.app.isPackaged
};

export function generateAlphaNumericString(length: number = 0): string {
  if (length <= 0) {
    throw new Error('Length must be greater than 0');
  }

  const characters = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';
  let result = '';
  for (let i = 0; i < length; i++) {
    result += characters.charAt(Math.floor(Math.random() * characters.length));
  }
  return result;
}
