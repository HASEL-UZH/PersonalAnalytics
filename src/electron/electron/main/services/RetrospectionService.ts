import { UserInputEntity } from '../entities/UserInputEntity'
import { WindowActivityEntity } from '../entities/WindowActivityEntity'
import { getMainLogger } from '../../config/Logger'

const LOG = getMainLogger('RetrospectionService')

export interface TimeActive {
  from: Date
  to: Date
  duration: number
}

export interface ActivitySessions {
  type: string
  totalDurationMs: number
  sessions: TimeActive[]
}

/**
 * converts a date object to a minute of the day (0-1439)
 * @param date - date object
 * @returns the minute of the day (0-1439)
 */
function getMinuteOfDay(date: Date): number {
  return date.getHours() * 60 + date.getMinutes()
}

/**
 * constructs a date object from a minute of the day (0-1439)
 * @param minuteOfDay - minute of the day (0-1439)
 * @returns a date object
 */
function getDateFromMinuteOfDay(minuteOfDay: number, baseDate?: Date): Date {
  const d = baseDate ? new Date(baseDate) : new Date()
  d.setHours(Math.floor(minuteOfDay / 60), minuteOfDay % 60, 0, 0)
  return d
}

/**
 * finds and returns all minutes of the day (0-1439) where user input was detected
 * @param date - date to check
 * @returns a Set of active minutes for the given day; minute encoded from 0 to 1439
 */
async function getActiveMinutesSet(date: Date | string): Promise<Set<number>> {
  const d = typeof date === 'string' ? new Date(date) : date
  const daystr = d.toISOString().split('T')[0] // e.g 2025-02-28
  // get all user input entries for the day in local timezone
  const userInputToday = await UserInputEntity.createQueryBuilder('userInput')
    .select([
      'userInput.*',
      // Convert UTC timestamp to local time using strftime
      "datetime(userInput.tsStart, 'localtime') as tsStart"
    ])
    .where('date(userInput.tsStart, \'localtime\') = :daystr', { daystr })
    .orderBy('userInput.tsStart', 'ASC')
    .getRawMany()

  // set includes all entries with at least one user input
  const activeMinutesSet: Set<number> = new Set()
  userInputToday
    .forEach((el) => {
      if (el.clickTotal > 0 || el.keysTotal > 0 || el.scrollDelta > 0 || el.movedDistance > 0) {
        activeMinutesSet.add(getMinuteOfDay(new Date(el.tsStart)))
      }
    })

  return activeMinutesSet
}

/**
 * @param date - date to get window activities for
 * @returns all window activities for the given day
 */
export async function getWindowActivities(date: Date | string): Promise<WindowActivityEntity[]> {
  const d = typeof date === 'string' ? new Date(date) : date
  const daystr = d.toISOString().split('T')[0]

  let res = await WindowActivityEntity.createQueryBuilder('windowActivity')
    .select([
      'windowActivity.*',
      "datetime(windowActivity.ts, 'localtime') as ts"
    ])
    .where('date(windowActivity.ts, \'localtime\') = :daystr', { daystr })
    .orderBy('windowActivity.ts', 'ASC')
    .getRawMany()

  return res
    .map((el) => ({ ...el, ts: new Date(el.ts) })) // convert ts to Date object
}

/**
 * adds an entry to the sessions map for a given key
 * @param map - the map to add the entry to
 * @param key - the key to group by (e.g., processName or activity)
 * @param from - the start date of the session
 * @param to - the end date of the session
 */
function addActivitySessionEntry(map: Map<string, ActivitySessions>, key: string | null, from: Date, to: Date) {
  if (!key) {
    LOG.warn("No `key` found for window activity, skipping entry")
    return
  }
  const entry = map.get(key) || { type: key, totalDurationMs: 0, sessions: [] } as ActivitySessions
  const duration = to.getTime() - from.getTime()
  let session = { from, to, duration }
  entry.sessions.push(session)
  entry.totalDurationMs += duration
  map.set(key, entry)
}

/**
 * active window activity sessions for apps or activities (e.g., "Planning", "InstantMessaging", ...)
 * @param prop - the property to group by, either "processName" for apps or "activity" for activities
 * @returns all usage sessions per prop, including the total duration
 */
async function getWindowActivitySessionsByType(prop: "processName" | "activity", date: Date): Promise<ActivitySessions[]> {
  const windowActivityToday = await getWindowActivities(date)
  const activeMinutesSet = await getActiveMinutesSet(date)
  // encodes session per processName (=app)
  const sessionsMap: Map<string, ActivitySessions> = new Map()
  // helper function to add an entry to the sessionsMap
  const addEntry = addActivitySessionEntry.bind(undefined, sessionsMap)

  // reconstruct the day so far by iterating over the window activities
  let lastWindowActivity: WindowActivityEntity | undefined = undefined
  for (let activity of windowActivityToday) {
    if (!activeMinutesSet.has(getMinuteOfDay(new Date(activity.ts)))) {
      // found window activity during a minute without any logged user input
      // skip for safety
      continue
    }

    if (lastWindowActivity && lastWindowActivity[prop] !== activity[prop]) {
      // we found a new window activity for a different app
      const app = lastWindowActivity[prop]
      const end = new Date(activity.ts)
      let start = new Date(lastWindowActivity.ts)

      const startMinute = getMinuteOfDay(start)
      const endMinute = getMinuteOfDay(end)

      let from = start
      if (startMinute + 1 < endMinute) {
        // the session is longer than 1 minute. Let's see if the session is interrupted by
        // inactive phases where the user may have left the computer or fell asleep
        let inSession = true
        // check all minutes in between
        for (let m = startMinute; m < endMinute; m++) {
          if (!activeMinutesSet.has(m) && inSession) {
            // session interrupted
            inSession = false
            let to = getDateFromMinuteOfDay(m, date)
            to = to.getTime() > end.getTime() ? end : to
            addEntry(app, from, to)
          } else if (!activeMinutesSet.has(m) && !inSession) {
            // session was already interrupted, and not yet resumed
            from = getDateFromMinuteOfDay(m, date)
          } else if (activeMinutesSet.has(m) && !inSession) {
            // session was interrupted, but user is active again
            from = getDateFromMinuteOfDay(m, date)
            inSession = true
          } else if (activeMinutesSet.has(m) && inSession) {
            // session is continuously active
          } else {
            LOG.error("Unexpected state in session reconstruction")
          }
        }
      }

      addEntry(app, from, end)
      lastWindowActivity = activity
    }

    if (!lastWindowActivity) {
      // initialize the lastWindowActivity in the first iteration
      lastWindowActivity = activity
    }
  }

  return Array.from(sessionsMap.values())
}

/**
 * finds the longest time period of the given day where user input was detected continuously
 * @returns the longest active time period
 */
export async function getLongestTimeActiveInsight(date: Date): Promise<TimeActive> {
  const activeMinutesSet = await getActiveMinutesSet(date) // encoded from 0 to 1439

  let longest: TimeActive = { from: new Date(), to: new Date(), duration: -1 }
  let periodStart: number | undefined = undefined
  for (let m = 0; m < 24 * 60; m++) {
    if (activeMinutesSet.has(m) && !periodStart) {
      periodStart = m
    } else if (!activeMinutesSet.has(m) && periodStart) {
      const duration = m - periodStart
      if (duration > longest.duration) {
        longest = {
          from: getDateFromMinuteOfDay(periodStart, date),
          to: getDateFromMinuteOfDay(m, date),
          duration
        }
      }
      periodStart = undefined
    }
  }

  return longest
}

/**
 * app usage sessions of the given day
 * @returns all usage sessions per app and the total duration
 */
export async function getAppUsageSessions(date: Date): Promise<ActivitySessions[]> {
  return await getWindowActivitySessionsByType("processName", date)
}

/**
 * activity sessions of the given day
 * @param date - the day to get activity sessions for
 * @param excludeUnspecificActivities - if true, excludes activities that are not specified (e.g., "Other")
 * @returns all activity sessions per type (e.g., "Planning", "InstantMessaging", ...) and the total duration
 */
export async function getActivitySessions(date: Date, excludeUnspecificActivities = true): Promise<ActivitySessions[]> {
  const sessions = await getWindowActivitySessionsByType("activity", date)
  if (excludeUnspecificActivities) {
    return sessions.filter(s => [
      "DevCode",
      "DevDebug",
      "DevReview",
      "DevVc",
      "Planning",
      "ReadWriteDocument",
      "Design",
      "GenerativeAI",
      "PlannedMeeting",
      "Email",
      "InstantMessaging",
      "WorkRelatedBrowsing",
      "WorkUnrelatedBrowsing",
      "SocialMedia",
      "FileManagement"].includes(s.type))
  }
  return sessions
}
