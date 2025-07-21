<script setup lang="ts">
import { PropType } from 'vue';
import ExperienceSamplingDto from '../../shared/dto/ExperienceSamplingDto';

defineProps({
  data: {
    type: Object as PropType<ExperienceSamplingDto[]>,
    default: null,
    required: false
  }
});
</script>
<template>
  <div class="my-5 border border-slate-400 p-2">
    <div class="prose max-w-none">
      <h2>Your Self Reported data</h2>
      <p>
        Your responses to the experience sampling questions will also be shared with the
        researchers. They do <b>not</b> contain any sensitive data.</p>
        <p>Here is a sample of your unmodified data:</p>
    </div>
    <div class="max-h-48 overflow-auto">
      <table
        class="table table-zebra table-pin-rows w-full overflow-auto text-xs"
      >
        <thead class="border-b">
          <tr>
            <th>Question</th>
            <th>Response</th>
            <th>Scale</th>
            <th>Response Options</th>
            <th>Skipped</th>
            <th>Prompted At</th>
          </tr>
        </thead>
        <tbody class="">
          <tr v-for="d in data" :key="d.id">
            <td>{{ d.question }}</td>
            <td>{{ d.response }}</td>
            <td>{{ d.scale }}</td>
            <td>{{ d.responseOptions }}</td>
            <td>{{ d.skipped }}</td>
            <td>{{ d.promptedAt.toLocaleString() }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>
