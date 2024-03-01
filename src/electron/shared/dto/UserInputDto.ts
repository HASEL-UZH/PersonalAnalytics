export default interface UserInputDto {
  id: string;
  keysTotal: number;
  clickTotal: number;
  movedDistance: number;
  scrollDelta: number;
  tsStart: Date;
  tsEnd: Date;
  createdAt: Date;
  updatedAt: Date;
  deletedAt: Date | null;
}
