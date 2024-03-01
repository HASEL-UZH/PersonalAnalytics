import { UserInputEntity } from '../../entities/UserInputEntity';
import UserInputAggregate from 'user-input-tracker/dist/types/UserInputAggregate';
import UserInputDto from '../../../../shared/dto/UserInputDto';

export class UserInputTrackerService {
  public static async handleUserInputEvent(userInputAggregate: UserInputAggregate): Promise<void> {
    await UserInputEntity.save({
      keysTotal: userInputAggregate.keyTotal,
      clickTotal: userInputAggregate.clickTotal,
      movedDistance: userInputAggregate.movedDistance,
      scrollDelta: userInputAggregate.scrollDelta,
      tsStart: userInputAggregate.tsStart,
      tsEnd: userInputAggregate.tsEnd
    });
  }

  public async getMostRecentUserInputDtos(itemCount: number): Promise<UserInputDto[]> {
    const entities = await UserInputEntity.find({
      order: { tsEnd: 'DESC' },
      take: itemCount
    });
    return entities.map((entity: UserInputEntity) => {
      return {
        keysTotal: entity.keysTotal,
        clickTotal: entity.clickTotal,
        movedDistance: entity.movedDistance,
        scrollDelta: entity.scrollDelta,
        tsStart: entity.tsStart,
        tsEnd: entity.tsEnd,
        id: entity.id,
        createdAt: entity.createdAt,
        updatedAt: entity.updatedAt,
        deletedAt: entity.deletedAt
      };
    });
  }
}
