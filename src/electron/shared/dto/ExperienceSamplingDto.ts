export default interface ExperienceSamplingDto {
  id: string;
  question: string;
  responseOptions: string;
  scale: number;
  response: number | null;
  skipped: boolean;
  promptedAt: Date;
  createdAt: Date;
  updatedAt: Date;
  deletedAt: Date | null;
}
