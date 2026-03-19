export default interface UserInputDto {
  id: string;
  clickTotal: number;
  movedDistance: number;
  scrollDelta: number;
  tsStart: Date;
  tsEnd: Date;
  createdAt: Date;
  updatedAt: Date;
  deletedAt: Date | null;

  keysLetter: number;
  keysNumber: number;
  keysNavigate: number;
  keysDelete: number;
  keysModifier: number;
  keysSpace: number;
  keysTab: number;
  keyEnter: number;
  keysOther: number;
  keysTotal: number;
}
