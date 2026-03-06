import { UserInputEntity } from '../../entities/UserInputEntity';
import UserInputAggregate from 'user-input-tracker/dist/types/UserInputAggregate';
import UserInputDto from '../../../../shared/dto/UserInputDto';

export class UserInputTrackerService {
  public static async handleUserInputEvent(userInputAggregate: UserInputAggregate): Promise<void> {
    const normalizedAggregate = userInputAggregate as UserInputAggregate & {
      keysTotal?: number;
      keysLetter?: number;
      keysNumber?: number;
      keysNavigate?: number;
      keysDelete?: number;
      keysModifier?: number;
      keysSpace?: number;
      keysTab?: number;
      keyEnter?: number;
      keysOther?: number;
    };

    await UserInputEntity.save({
      clickTotal: normalizedAggregate.clickTotal ?? 0,
      movedDistance: normalizedAggregate.movedDistance ?? 0,
      scrollDelta: normalizedAggregate.scrollDelta ?? 0,
      tsStart: userInputAggregate.tsStart,
      tsEnd: userInputAggregate.tsEnd,

      keysLetter: normalizedAggregate.keysLetter ?? 0,
      keysNumber: normalizedAggregate.keysNumber ?? 0,
      keysNavigate: normalizedAggregate.keysNavigate ?? 0,
      keysDelete: normalizedAggregate.keysDelete ?? 0,
      keysModifier: normalizedAggregate.keysModifier ?? 0,
      keysSpace: normalizedAggregate.keysSpace ?? 0,
      keysTab: normalizedAggregate.keysTab ?? 0,
      keyEnter: normalizedAggregate.keyEnter ?? 0,
      keysOther: normalizedAggregate.keysOther ?? 0,
      keysTotal: normalizedAggregate.keysTotal ?? normalizedAggregate.keyTotal ?? 0
    });
  }

  public async getMostRecentUserInputDtos(itemCount: number): Promise<UserInputDto[]> {
    const entities = await UserInputEntity.find({
      order: { tsEnd: 'DESC' },
      take: itemCount
    });

    return entities.map((entity: UserInputEntity) => ({
      clickTotal: entity.clickTotal ?? 0,
      movedDistance: entity.movedDistance ?? 0,
      scrollDelta: entity.scrollDelta ?? 0,
      tsStart: entity.tsStart,
      tsEnd: entity.tsEnd,
      id: entity.id,
      createdAt: entity.createdAt,
      updatedAt: entity.updatedAt,
      deletedAt: entity.deletedAt,

      keysLetter: entity.keysLetter ?? 0,
      keysNumber: entity.keysNumber ?? 0,
      keysNavigate: entity.keysNavigate ?? 0,
      keysDelete: entity.keysDelete ?? 0,
      keysModifier: entity.keysModifier ?? 0,
      keysSpace: entity.keysSpace ?? 0,
      keysTab: entity.keysTab ?? 0,
      keyEnter: entity.keyEnter ?? 0,
      keysOther: entity.keysOther ?? 0,
      keysTotal: entity.keysTotal ?? 0
    }));
  }
}
