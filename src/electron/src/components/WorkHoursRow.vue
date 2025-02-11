<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { hourList } from './hoursList';
import {WorkHoursDto, WorkHoursDayDto} from '../../shared/dto/WorkHoursDto'
import Switch from './Switch.vue'

const props = defineProps<{
  day: keyof WorkHoursDto;
  preSetWorkHours: WorkHoursDayDto;
  setWorkHours: (day: keyof WorkHoursDto, workHoursDay: WorkHoursDayDto) => Promise<void>;
}>();

watch(() => props.preSetWorkHours, (newWorkHours) => {
  isEnabled.value = newWorkHours.isWorking;
  startTime.value = newWorkHours.startTime;
  endTime.value = newWorkHours.endTime;
});

// Refs for reactive variables
const isEnabled = ref(props.preSetWorkHours.isWorking);
const startTime = ref(props.preSetWorkHours.startTime);
const endTime = ref(props.preSetWorkHours.endTime);

// Helper functions
const timeStringToMinutes = (timeStr: string): number => {
  const [hours, minutes] = timeStr.split(':').map(Number);
  return hours * 60 + minutes;
};

const capitalizeFirstLetter = (str: string): string => {
  return str.charAt(0).toUpperCase() + str.slice(1);
}

// Computed values for time options
const startTimeOptions = computed(() =>
  hourList.filter((time) => timeStringToMinutes(time) < timeStringToMinutes(endTime.value))
);

const endTimeOptions = computed(() =>
  hourList.filter((time) => timeStringToMinutes(time) > timeStringToMinutes(startTime.value))
);

// Event handlers
const onChangeWorkdayIsEnabled = async (e: Event) => {
  const isChecked = (e.target as HTMLInputElement).checked;
  const updatedWorkHours: WorkHoursDayDto = {
    ...props.preSetWorkHours,
    isWorking: isChecked,
  };
  await props.setWorkHours(props.day, updatedWorkHours);
};

const onSelectStartChange = async () => {
  const updatedWorkHours: any = {
    ...props.preSetWorkHours,
    startTime: startTime.value,
  };

  await props.setWorkHours(props.day, updatedWorkHours);
};

const onSelectEndChange = async () => {
  const updatedWorkHours: any = {
    ...props.preSetWorkHours,
    endTime: endTime.value,
  };

  await props.setWorkHours(props.day, updatedWorkHours);
};
</script>

<template>
  <div class="z-10 mt-10 mb-10 flex items-center fix-height">
    <div class="outer-switch-container">
      <Switch :modelValue="isEnabled" :label="capitalizeFirstLetter(props.day)" :on-change="onChangeWorkdayIsEnabled" />
    </div>

    <div v-if="isEnabled" class="time-selectors">
      <span>From:</span>
      <select class="ml-2 p-1" v-model="startTime" @change="onSelectStartChange">
        <option
          v-for="time in startTimeOptions"
          :key="time"
          :value="time"
        >
          {{ time }}
        </option>
      </select>

      <span>To:</span>
      <select class="ml-2 p-1" v-model="endTime" @change="onSelectEndChange"> 
        <option
          v-for="time in endTimeOptions"
          :key="time"
          :value="time"
        >
          {{ time }}
        </option>
      </select>
    </div>

    <div v-else class="not-working">
      <span>Not active on this device</span>
    </div>
  </div>
</template>


<style scoped>
.time-selectors select {
  margin-right: 1rem;
}

.fix-height {
  height: 18px;
}

.not-working {
  color: gray;
  font-style: italic;
}

.outer-switch-container {
  width: 200px;
}
</style>
