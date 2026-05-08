<template>
  <div class="job-post-form">
    <a-card>
      <a-form
        ref="formRef"
        :model="formState"
        :label-col="{ span: 4 }"
        :wrapper-col="{ span: 18 }"
        layout="horizontal"
      >
        <a-form-item label="职位名称" name="title" :rules="[{ required: true, message: '请输入职位名称' }]">
          <a-input v-model:value="formState.title" placeholder="请输入职位名称" />
        </a-form-item>

        <a-form-item label="所属部门" name="department">
          <a-input v-model:value="formState.department" placeholder="请输入所属部门" />
        </a-form-item>

        <a-form-item label="职位描述" name="description">
          <a-textarea v-model:value="formState.description" :rows="4" placeholder="请输入职位描述" />
        </a-form-item>

        <a-form-item label="任职要求" name="requirements">
          <a-textarea v-model:value="formState.requirements" :rows="4" placeholder="请输入任职要求" />
        </a-form-item>

        <a-form-item label="最低薪资">
          <a-input-number
            v-model:value="formState.salaryMin"
            :min="0"
            :step="1000"
            style="width: 200px"
            placeholder="请输入最低薪资"
          />
          <span style="margin-left: 8px">元/月</span>
        </a-form-item>

        <a-form-item label="最高薪资">
          <a-input-number
            v-model:value="formState.salaryMax"
            :min="0"
            :step="1000"
            style="width: 200px"
            placeholder="请输入最高薪资"
          />
          <span style="margin-left: 8px">元/月</span>
        </a-form-item>

        <a-form-item label="经验要求" name="experience">
          <a-select v-model:value="formState.experience" placeholder="请选择经验要求" allowClear>
            <a-select-option value="不限">不限</a-select-option>
            <a-select-option value="1年以下">1年以下</a-select-option>
            <a-select-option value="1-3年">1-3年</a-select-option>
            <a-select-option value="3-5年">3-5年</a-select-option>
            <a-select-option value="5-10年">5-10年</a-select-option>
            <a-select-option value="10年以上">10年以上</a-select-option>
          </a-select>
        </a-form-item>

        <a-form-item label="学历要求" name="education">
          <a-select v-model:value="formState.education" placeholder="请选择学历要求" allowClear>
            <a-select-option value="不限">不限</a-select-option>
            <a-select-option value="高中/中专">高中/中专</a-select-option>
            <a-select-option value="大专">大专</a-select-option>
            <a-select-option value="本科">本科</a-select-option>
            <a-select-option value="硕士">硕士</a-select-option>
            <a-select-option value="博士">博士</a-select-option>
          </a-select>
        </a-form-item>

        <a-form-item label="状态" name="status">
          <a-select v-model:value="formState.status" placeholder="请选择状态">
            <a-select-option value="Draft">草稿</a-select-option>
            <a-select-option value="Published">已发布</a-select-option>
            <a-select-option value="Paused">暂停</a-select-option>
            <a-select-option value="Closed">已关闭</a-select-option>
          </a-select>
        </a-form-item>

        <a-form-item :wrapper-col="{ offset: 4 }">
          <a-space>
            <a-button type="primary" @click="handleSubmit" :loading="loading">
              保存
            </a-button>
            <a-button @click="handleCancel">
              取消
            </a-button>
          </a-space>
        </a-form-item>
      </a-form>
    </a-card>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { jobPostApi } from '@/api/jobpost'
import { message } from 'ant-design-vue'

const router = useRouter()
const route = useRoute()
const formRef = ref()
const loading = ref(false)
const isEdit = ref(false)
const jobPostId = ref(null)

const formState = reactive({
  title: '',
  department: '',
  description: '',
  requirements: '',
  salaryMin: null,
  salaryMax: null,
  experience: undefined,
  education: undefined,
  status: 'Draft'
})

async function fetchData() {
  if (!jobPostId.value) return
  try {
    const res = await jobPostApi.getById(jobPostId.value)
    const data = res.data
    Object.assign(formState, {
      title: data.title || '',
      department: data.department || '',
      description: data.description || '',
      requirements: data.requirements || '',
      salaryMin: data.salaryMin || null,
      salaryMax: data.salaryMax || null,
      experience: data.experience || undefined,
      education: data.education || undefined,
      status: data.status || 'Draft'
    })
  } catch (e) {
    console.error(e)
    message.error('获取职位信息失败')
  }
}

async function handleSubmit() {
  try {
    await formRef.value.validate()
    loading.value = true
    if (isEdit.value) {
      await jobPostApi.update(jobPostId.value, formState)
      message.success('更新成功')
    } else {
      await jobPostApi.create(formState)
      message.success('创建成功')
    }
    router.push('/jobposts')
  } catch (e) {
    console.error(e)
  } finally {
    loading.value = false
  }
}

function handleCancel() {
  router.back()
}

onMounted(() => {
  if (route.path !== '/jobposts/new') {
    jobPostId.value = parseInt(route.params.id)
    isEdit.value = true
    fetchData()
  }
})
</script>

<style scoped>
.job-post-form {
  padding: 24px;
}
</style>
