<script setup lang="ts">

import { onMounted, ref } from 'vue'
// import { useRoute } from 'vue-router'
import WorkHoursRow from '../../components/WorkHoursRow.vue'
import typedIpcRenderer from '../../utils/typedIpcRenderer'
import {WorkHoursDayDto, WorkHoursDto} from '../../../shared/dto/WorkHoursDto'
import Switch from '../../components/Switch.vue'

// const route = useRoute()
// const isMacOS: string | null | LocationQueryValue[] = route.query.isMacOS

let workHoursRef = ref<WorkHoursDto>({
  monday: { startTime: '', endTime: '', isWorking: false },
  tuesday: { startTime: '', endTime: '', isWorking: false },
  wednesday: { startTime: '', endTime: '', isWorking: false },
  thursday: { startTime: '', endTime: '', isWorking: false },
  friday: { startTime: '', endTime: '', isWorking: false },
  saturday: { startTime: '', endTime: '', isWorking: false },
  sunday: { startTime: '', endTime: '', isWorking: false },
});

const isEnabled = ref(true);

onMounted(async () => {        
  try {
    const workHours = await typedIpcRenderer.invoke('getWorkHours') as WorkHoursDto
    workHoursRef.value = { ...workHours }

    isEnabled.value = await typedIpcRenderer.invoke('getWorkHoursEnabled') as boolean
  } catch (error) {
    console.error('Error getting work hours:', error)
  }
})

const updateWorkHours = async (day: keyof WorkHoursDto, updatedWorkHours: WorkHoursDayDto) => {
  workHoursRef.value[day] = updatedWorkHours;
  try {
    const serializableWorkHours = JSON.parse(JSON.stringify(workHoursRef.value));
    await typedIpcRenderer.invoke('setWorkHours', serializableWorkHours )
  } catch (error) {
    console.error('Error setting work hours:', error)
  }
};

const onChangeWorkHoursIsEnabled = async (e: Event) => {
  const isChecked = (e.target as HTMLInputElement).checked;
  isEnabled.value = isChecked;
  try {
    await typedIpcRenderer.invoke('setWorkHoursEnabled', isChecked)
  } catch (error) {
    console.error('Error setting work hours enabled:', error)
  }
};

</script>

<template>
  <div>
    <article class="prose prose-lg mt-4 mb-5">
      <h1>
        <span class="primary-blue">Active Times</span>
      </h1>
      <span>
        Define your active times for each day of the week, such as the time you usually work or study.
      </span>
      <span v-if="isEnabled">
        Outside these times, no experience sampling pop-up is shown.
      </span>
    </article>

    <Switch :modelValue="isEnabled" :label="'Enable/disable active work hours'" :on-change="onChangeWorkHoursIsEnabled" />

    <div v-if="isEnabled" class="work-hours-container">
      <div v-for="(workHoursDay, day) in workHoursRef" :key="day">
        <work-hours-row :day="day" :pre-set-work-hours="workHoursDay!" :set-work-hours="updateWorkHours" />
      </div>
    </div>

  </div>
</template>

<style lang="less">
@import '../../styles/index';
.primary-blue {
  color: @primary-color;
}

.work-hours-container {
  width: 70%;
  border-top: 1px solid rgb(59 130 246 / 0.5);
  margin-top: 40px;
}
</style>
