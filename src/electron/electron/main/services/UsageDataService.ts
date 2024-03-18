import getMainLogger from '../../config/Logger';
import { UsageDataEventType } from '../../enums/UsageDataEventType.enum';
import { UsageDataEntity } from '../entities/UsageDataEntity';

const LOG = getMainLogger('UsageDataService');

export class UsageDataService {
  public static async createNewUsageDataEvent(
    type: UsageDataEventType,
    additionalInformation?: string
  ): Promise<void> {
    LOG.debug(`Creating new usage data event of type ${type}`);
    await UsageDataEntity.save({
      type,
      additionalInformation
    });
  }
}
