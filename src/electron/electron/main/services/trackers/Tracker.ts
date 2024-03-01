export interface Tracker {
  name: string;
  isRunning: boolean;

  start(): void;

  resume?(): void;

  stop(): void;
}
