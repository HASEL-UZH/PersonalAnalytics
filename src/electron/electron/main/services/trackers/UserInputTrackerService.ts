import { UserInputEntity } from '../../entities/UserInputEntity';
import UserInputAggregate from 'user-input-tracker/dist/types/UserInputAggregate';

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

  public async getMostRecentUserInputs(itemCount: number): Promise<UserInputEntity[]> {
    return UserInputEntity.find({
      order: { tsEnd: 'DESC' },
      take: itemCount
    });
  }
}
