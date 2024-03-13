import { getLogger } from '../../shared/Logger';
import { UsageDataEventType } from '../../enums/UsageDataEventType.enum';
import { UsageDataEntity } from '../entities/UsageDataEntity';

const LOG = getLogger('UsageDataService');

export class UsageDataService {
  public async createNewUsageDataEvent(
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
