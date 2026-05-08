<template>
  <div class="resume-parse">
    <a-card title="简历解析" :bordered="false">
      <a-space direction="vertical" :size="16" style="width: 100%">
        <a-textarea
          v-model:value="resumeText"
          placeholder="请粘贴简历文本内容..."
          :rows="10"
          :maxlength="10000"
          show-count
        />
        <a-space>
          <a-button type="primary" :loading="parsing" :disabled="!resumeText.trim()" @click="handleParse">
            解析
          </a-button>
          <a-button @click="handleClear">
            清空
          </a-button>
        </a-space>
      </a-space>
    </a-card>

    <a-card v-if="parsedResult" title="解析结果" :bordered="false" style="margin-top: 16px">
      <a-descriptions bordered :column="2">
        <a-descriptions-item label="姓名">{{ parsedResult.name || '-' }}</a-descriptions-item>
        <a-descriptions-item label="手机号">{{ parsedResult.phone || '-' }}</a-descriptions-item>
        <a-descriptions-item label="邮箱">{{ parsedResult.email || '-' }}</a-descriptions-item>
        <a-descriptions-item label="工作年限">{{ parsedResult.totalWorkYears || '-' }} 年</a-descriptions-item>
      </a-descriptions>

      <a-divider>工作经历</a-divider>
      <a-timeline v-if="parsedResult.workExperience?.length">
        <a-timeline-item v-for="(exp, idx) in parsedResult.workExperience" :key="idx">
          <p><strong>{{ exp.company }}</strong> - {{ exp.position }}</p>
          <p v-if="exp.duration" style="color: #888; font-size: 12px">{{ exp.duration }}</p>
          <p v-if="exp.description">{{ exp.description }}</p>
        </a-timeline-item>
      </a-timeline>
      <a-empty v-else description="无工作经历信息" />

      <a-divider>教育背景</a-divider>
      <a-list v-if="parsedResult.education?.length" size="small" bordered>
        <a-list-item v-for="(edu, idx) in parsedResult.education" :key="idx">
          <a-list-item-meta>
            <template #title>{{ edu.school }}</template>
            <template #description>
              {{ edu.degree }} | {{ edu.major }}
              <span v-if="edu.graduationYear"> | {{ edu.graduationYear }}</span>
            </template>
          </a-list-item-meta>
        </a-list-item>
      </a-list>
      <a-empty v-else description="无教育背景信息" />

      <a-divider>技能标签</a-divider>
      <a-tag v-for="(skill, idx) in parsedResult.skillTags" :key="idx" color="blue">
        {{ skill }}
      </a-tag>
      <a-empty v-if="!parsedResult.skillTags?.length" description="无技能标签" />

      <a-divider>个人简介</a-divider>
      <p>{{ parsedResult.summary || '-' }}</p>

      <a-divider>Token使用</a-divider>
      <a-statistic title="MiniMax Token消耗" :value="parsedResult.minimaxTokens || 0" suffix="tokens" />

      <a-space style="margin-top: 24px">
        <a-button type="primary" @click="handleCreateCandidate">
          创建候选人
        </a-button>
      </a-space>
    </a-card>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { message } from 'ant-design-vue'

const resumeText = ref('')
const parsing = ref(false)
const parsedResult = ref(null)

async function handleParse() {
  if (!resumeText.value.trim()) return

  parsing.value = true
  try {
    const res = await fetch('/api/resumes/parse', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${localStorage.getItem('token') || ''}`
      },
      body: JSON.stringify({ resumeText: resumeText.value })
    })
    const data = await res.json()
    if (data.success) {
      parsedResult.value = data.data
      message.success('解析成功')
    } else {
      message.error(data.message || '解析失败')
    }
  } catch (e) {
    console.error(e)
    message.error('解析失败')
  } finally {
    parsing.value = false
  }
}

function handleClear() {
  resumeText.value = ''
  parsedResult.value = null
}

async function handleCreateCandidate() {
  if (!parsedResult.value) return

  try {
    const res = await fetch('/api/candidates', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${localStorage.getItem('token') || ''}`
      },
      body: JSON.stringify({
        name: parsedResult.value.name,
        phone: parsedResult.value.phone,
        email: parsedResult.value.email,
        skills: parsedResult.value.skillTags?.join(','),
        education: parsedResult.value.education?.[0]
          ? `${parsedResult.value.education[0].school} - ${parsedResult.value.education[0].major}`
          : null,
        workExperience: parsedResult.value.totalWorkYears
      })
    })
    const data = await res.json()
    if (data.success) {
      message.success('候选人创建成功')
    } else {
      message.error(data.message || '创建失败')
    }
  } catch (e) {
    console.error(e)
    message.error('创建失败')
  }
}
</script>

<style scoped>
.resume-parse {
  padding: 24px;
}
</style>