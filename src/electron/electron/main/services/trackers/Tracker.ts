export interface Tracker {
  name: string;
  isRunning: boolean;

  start(): void;

  stop(): void;

  terminate?(): Promise<void>;
}
