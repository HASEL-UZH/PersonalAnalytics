export default interface WindowActivityDto {
  id: string;
  windowTitle: string | null;
  processName: string | null;
  processPath: string | null;
  processId: number | null;
  url: string | null;
  activity: string;
  ts: Date;
  createdAt: Date;
  updatedAt: Date;
  deletedAt: Date | null;
}
