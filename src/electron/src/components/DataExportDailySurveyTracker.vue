<script setup lang="ts">
import { PropType } from 'vue';
import DailySurveyDto from '../../shared/dto/DailySurveyDto';

defineProps({
  data: {
    type: Object as PropType<DailySurveyDto[]>,
    default: null,
    required: false
  }
});

function formatResponse(d: DailySurveyDto): string {
  if (!d.response) return '';
  if (d.answerType === 'MultiChoice') {
    try {
      const parsed = JSON.parse(d.response) as string[];
      return parsed.join(', ');
    } catch {
      return d.response;
    }
  }
  return d.response;
}

function formatResponseOptions(d: DailySurveyDto): string {
  if (!d.responseOptions) return '';
  try {
    const parsed = JSON.parse(d.responseOptions);
    if (parsed && typeof parsed === 'object' && 'type' in parsed) {
      if (parsed.type === 'LikertScale') {
        return `${parsed.scale}-point: ${(parsed.labels as string[]).join(', ')}`;
      }
      if (parsed.type === 'TextResponse') {
        return `${parsed.inputType}, max ${parsed.maxLength}`;
      }
      if (parsed.options) {
        return (parsed.options as string[]).join(', ');
      }
    }
    if (Array.isArray(parsed)) {
      return parsed.join(', ');
    }
    return d.responseOptions;
  } catch {
    return d.responseOptions;
  }
}
</script>
<template>
  <div class="my-5 border border-slate-400 p-2">
    <div class="prose max-w-none">
      <h2>Your Daily Survey data</h2>
      <p>
        Your responses to the daily survey questions will also be shared with the
        researchers. They do <b>not</b> contain any sensitive data.</p>
        <p>Here is a sample of your unmodified data:</p>
    </div>
    <div class="max-h-48 overflow-auto">
      <table
        class="table table-zebra table-pin-rows w-full overflow-auto text-xs"
      >
        <thead class="border-b">
          <tr>
            <th>Sampling Type</th>
            <th>Question</th>
            <th>Answer Type</th>
            <th>Response</th>
            <th>Scale</th>
            <th>Response Options</th>
            <th>Skipped</th>
            <th>Created At</th>
          </tr>
        </thead>
        <tbody class="">
          <tr v-for="d in data" :key="d.id">
            <td>{{ d.samplingType }}</td>
            <td>{{ d.question }}</td>
            <td>{{ d.answerType }}</td>
            <td>{{ formatResponse(d) }}</td>
            <td>{{ d.scale }}</td>
            <td>{{ formatResponseOptions(d) }}</td>
            <td>{{ d.skipped }}</td>
            <td>{{ d.createdAt.toLocaleString() }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>
