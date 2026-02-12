import { UserInputEntity } from '../../entities/UserInputEntity';
import UserInputAggregate from 'user-input-tracker/dist/types/UserInputAggregate';
import UserInputDto from '../../../../shared/dto/UserInputDto';

export class UserInputTrackerService {
  public static async handleUserInputEvent(userInputAggregate: UserInputAggregate): Promise<void> {
    const aggAny = userInputAggregate as any;

    await UserInputEntity.save({
      keysTotal: userInputAggregate.keyTotal,
      clickTotal: userInputAggregate.clickTotal,
      movedDistance: userInputAggregate.movedDistance,
      scrollDelta: userInputAggregate.scrollDelta,
      tsStart: userInputAggregate.tsStart,
      tsEnd: userInputAggregate.tsEnd,

      keysLetter: aggAny.keysLetter ?? 0,
      keysNumber: aggAny.keysNumber ?? 0,
      keysNavigate: aggAny.keysNavigate ?? 0,
      keysDelete: aggAny.keysDelete ?? 0,
      keysModifier: aggAny.keysModifier ?? 0,
      keysSpace: aggAny.keysSpace ?? 0,
      keysTab: aggAny.keysTab ?? 0,
      keyEnter: aggAny.keyEnter ?? 0,
      keysOther: aggAny.keysOther ?? 0,
    });
  }

  public async getMostRecentUserInputDtos(itemCount: number): Promise<UserInputDto[]> {
    const entities = await UserInputEntity.find({
      order: { tsEnd: 'DESC' },
      take: itemCount
    });

    return entities.map((entity: UserInputEntity) => ({
      keysTotal: entity.keysTotal,
      clickTotal: entity.clickTotal,
      movedDistance: entity.movedDistance,
      scrollDelta: entity.scrollDelta,
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
    }));
  }
}