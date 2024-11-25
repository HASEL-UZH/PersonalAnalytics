# PersonalAnalytics - Electron 

## Installation

Before running `npm install`, initialize the git submodules as follows:

```bash
git submodule init
git submodule update
```
## External Component: Goal Setting/Task Tracking
To enable this component:
1. clone `PA.SelfReflection` (private) and add it in the parent folder of `PersonalAnalytics` (as siblings).
2. set `trackers.taskTracker.enabled` to `true` in the [study config](shared/study.config.ts).

If any of the two steps are not complete, the code should savely exclude the goal-setting functionality.
For more information, see [here](external/README.md)

## Issues
https://github.com/HASEL-UZH/PA.SelfReflection/issues


## Ask Sebastian
- unresolved imports https://royru.ch/#unresolved-inputs
  - do you see the same erros on your side?
- electron toolkit @preload - what is it for, and is it needed? 
  - hab's mittlerweile entfernt, ist es nötig?
- Parsing Error https://royru.ch/#Parsing-error
- What is commands.ts used for?

## André
- How much tracking do we need for TaskPlanner (Taskbar Open/Close, ...)
- Naming: SelfReflection/GoalSetting/TaskTracker
- Logic of Goal Setting External Component
- WindowActivityEntity - startDate, endDate, durationInMs
- What about AW Store + TaskTrackingReminders

## TODOS - 2024-11-25
- [DONE] UI for planner and taskview is somewhat broken 
  - [DONE] basic ui fix 
  - [DONE] some functionality in the UI is also broken (start/stop task, ...)
- [DONE] fallback for when @external (tmp-external) is missing
- [DONE] verify all actions in the ipcHandler https://royru.ch/#actions
  - [DONE] also move new actions into the PA.SelfReflection repo.
  - ~~also check Commands.ts logic --> where is it used? How to combine with external Commands~~
- [DONE] task/planner scheduling logic from Awarenessbar as TaskTracker
- [ ] ColorPicker doesn't work
- [ ] double check the api under preload/index.ts
    onLoadTaskbarTasks: (cb) => void;
    onRemindToTrackTime: (cb) => void;
    onTaskWidgetWindowFocused: (cb) => void;
    isMacOS: () => boolean;

