import electron from 'electron';

export declare interface Is {
  dev: boolean;
}

export const is: Is = {
  dev: !electron.app.isPackaged
};
