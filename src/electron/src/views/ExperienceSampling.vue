<script lang="ts" setup></script>
<template>
  <div class="experience-sampling-notification">
    <template v-if="true">
      <div class="notification-top-bar">
        <div>Self-Report: asdf</div>
        <div>asdf</div>
      </div>
      <div class="pointer-events-auto flex w-full">
        <div class="w-0 flex-1 p-4 pt-1">
          <div class="flex items-start">
            <div class="w-0 flex-1">
              <p class="prompt">asdf</p>
              <div class="-mx-2 mt-2 flex flex-row justify-between">
                <div
                  v-for="value in [1, 2, 3, 4, 5, 6, 7]"
                  :key="value"
                  class="sample-answer"
                  @click=""
                >
                  <span v-if="true" class="mx-auto flex font-medium">
                    {{ value }}
                  </span>
                </div>
              </div>
              <div class="mt-1 flex flex-row text-sm text-gray-400">
                <div class="">asdf</div>
                <div class="mx-auto">asdf</div>
                <div class="">asdf</div>
              </div>
            </div>
          </div>
        </div>
        <div class="flex cursor-pointer border-l border-gray-200" @click="">
          <div
            class="flex w-full items-center justify-center rounded-none rounded-r-lg border border-transparent p-4 text-sm font-medium text-gray-600 hover:text-gray-900 focus:outline-none"
          >
            Skip
          </div>
        </div>
      </div>
    </template>
  </div>
</template>
<style lang="less" scoped>
.experience-sampling-notification {
  user-select: none;
  overflow: hidden;

  .notification-top-bar {
    @apply pointer-events-auto flex w-full justify-between bg-gray-200 px-2 py-1 text-xs text-gray-500;
    line-height: 1.35rem;
    -webkit-app-region: drag;
  }

  .prompt {
    @apply font-medium text-gray-900;
  }

  .sample-answer {
    @apply mx-1 flex h-8 w-8 cursor-pointer items-center rounded-md border border-gray-200 bg-gray-100 hover:bg-gray-700 text-center align-middle text-gray-500 hover:text-white outline-none ring-2 ring-gray-900 transition-all;
  }
}
</style>
