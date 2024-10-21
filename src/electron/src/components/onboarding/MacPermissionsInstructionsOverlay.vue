<script setup lang="ts">
import { defineProps } from 'vue';
defineProps<{
  showAccessibility: boolean;
}>();
</script>
<template>
  <div class="text-md">
    <p>If you are not presented with the dialog:</p>
    <p>
      Open the <span class="font-bold text-slate-200">Mac menu</span> and select
      <span class="font-bold text-slate-200">System Settings...</span>
    </p>
    <div class="my-3 flex items-center justify-center">
      <img src="../../assets/onboarding/permissions_systemsettings.png" class="w-1/3" />
    </div>
    <p>
      Select the
      <span class="font-bold text-slate-200">Privacy & Security</span> entry on the left side and
      then navigate to each of the following sections within the list (Accessibility, Screen &
      System Audio Recording).
    </p>
    <div class="flex items-center justify-center">
      <img src="../../assets/onboarding/permissions_privacy.png" class="w-3/4" />
    </div>
    <p v-if="showAccessibility">
      When selecting
      <span class="font-bold text-slate-200">Accessibility or Screen & System Audio Recording</span>
      you might be prompted to enter your administrator account credentials. Please do so to
      proceed. Check the slider next to PersonalAnalytics to grant it the necessary permissions.
    </p>
    <p v-if="showAccessibility" class="mt-6">
      After giving permissions, you will be asked to close and restart PersonalAnalytics. Please
      restart the app manually if it doesn't do so automatically.
    </p>
  </div>
</template>
