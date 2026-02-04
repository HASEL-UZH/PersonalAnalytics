<script lang="ts" setup>
import { onMounted, ref, computed } from 'vue'
import Switch from '../../components/Switch.vue'
import studyConfig from '../../../shared/study.config'
import typedIpcRenderer from '../../utils/typedIpcRenderer'

const es = studyConfig.trackers.experienceSamplingTracker
const allowUserToDisable = es.allowUserToDisable ?? true
const allowUserToChangeInterval = es.allowUserToChangeInterval ?? true

const disabled = ref(false)
const selectedInterval = ref<number | null>(null)

const intervalOptions = computed<number[]>(
  () => (es.userDefinedInterval_h ?? []).filter((n: number) => Number.isFinite(n))
)
const questions = es.questions

const defaultIntervalHours = es.intervalInMs / (1000 * 60 * 60)

function formatHours(h: number): string {
  return h < 1 ? `${Math.round(h * 60)} minutes` : `${h} hour(s)`
}

function pickClosestOption(target: number, options: number[], eps = 1e-6): number | null {
  if (!options.length) return null
  const exact = options.find(o => Math.abs(o - target) < eps)
  if (exact !== undefined) return exact
  return options.reduce((a, b) => (Math.abs(b - target) < Math.abs(a - target) ? b : a))
}

const selectedDropdownValue = computed<number | ''>(() => {
  if (selectedInterval.value !== null) return selectedInterval.value
  const closest = pickClosestOption(defaultIntervalHours, intervalOptions.value)
  return (closest ?? '') as number | ''
})

async function load() {
  const settings: any = await typedIpcRenderer.invoke('getSettings')
  disabled.value = (settings.userDisabledExperienceSampling ?? 0) === 1
  selectedInterval.value =
    settings.userDefinedExperienceSamplingInterval_h ?? null
}

const onChangeSelfReportingEnabled = async (e: Event) => {
  const isChecked = (e.target as HTMLInputElement).checked
  disabled.value = !isChecked
  await typedIpcRenderer.invoke(
    'setSettingsProp',
    'userDisabledExperienceSampling',
    disabled.value ? 1 : 0
  )
}

const onSelectInterval = async (val: string) => {
  const v = val === '' ? null : Number(val)
  selectedInterval.value = v
  await typedIpcRenderer.invoke(
    'setSettingsProp',
    'userDefinedExperienceSamplingInterval_h',
    v
  )
}

onMounted(load)
</script>

<template>
  <div>
    <article class="prose prose-lg mt-4 mb-5">
      <h1 class="mt-0">
        <span class="primary-blue">Self-Reflection</span>
      </h1>
      <p class="text-base">
        PersonalAnalytics allows you to periodically reflect.
      </p>
    </article>

    <article class="prose prose-lg mt-4">
      <div v-if="allowUserToDisable" class="mb-6">
        <Switch
          :modelValue="!disabled"
          :label="'Enable/disable periodic self-reflection'"
          :on-change="onChangeSelfReportingEnabled"
        />
      </div>

      <div
        v-if="allowUserToChangeInterval && intervalOptions.length > 0"
        class="self-reporting-container"
      >
        <div class="form-control w-[70%] max-w-xl">
          <label class="label pb-0">
            <span class="label-text text-base">
              How frequently would you like to reflect (during active times)?
            </span>
          </label>
          <select
            class="select select-bordered mt-2"
            :value="selectedDropdownValue"
            @change="onSelectInterval(($event.target as HTMLSelectElement).value)"
          >
            <option v-for="h in intervalOptions" :key="h" :value="h">
              {{ formatHours(h) }}
            </option>
          </select>
        </div>
      </div>
    </article>

    <article class="prose prose-lg mt-4">
      <div class="self-reporting-container">
        <div class="font-medium mb-2">Self-Reflection Questions:</div>
        <ul class="list-disc ml-6">
          <li v-for="q in questions" :key="q">{{ q }}</li>
        </ul>
      </div>
    </article>
  </div>
</template>

<style lang="less" scoped>
@import '../../styles/index';

.primary-blue {
  color: @primary-color;
}

.self-reporting-container {
  width: 90%;
  border-top: 1px solid rgb(59 130 246 / 0.5);
  margin-top: 24px;
  padding-top: 16px;
}
</style>