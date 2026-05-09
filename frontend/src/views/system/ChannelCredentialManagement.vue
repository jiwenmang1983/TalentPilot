<template>
  <div class="channel-credential-management">
    <!-- Page Header -->
    <div class="page-header">
      <div class="page-header-left">
        <h1>🔗 渠道账号管理</h1>
        <p>配置各招聘渠道的接入凭证，支持 API Key、浏览器自动化和自定义链接三种接入方式</p>
      </div>
    </div>

    <!-- Channel Cards -->
    <div class="channel-grid">
      <div
        v-for="channel in channelList"
        :key="channel.channelType"
        class="channel-card"
        :class="{ 'channel-disabled': !channel.isEnabled }"
      >
        <div class="channel-card-header">
          <div class="channel-icon">{{ channel.icon }}</div>
          <div class="channel-info">
            <div class="channel-name">{{ channel.channelName }}</div>
            <div class="channel-type-badge">{{ channel.accessTypeLabel }}</div>
          </div>
          <div class="channel-status">
            <a-tag :color="channel.isEnabled ? 'green' : 'default'">
              {{ channel.isEnabled ? '已启用' : '已禁用' }}
            </a-tag>
          </div>
        </div>

        <div class="channel-card-body">
          <div class="channel-credential-preview">
            <span class="label">凭证状态：</span>
            <span class="value">{{ channel.maskedCredentials || '未配置' }}</span>
          </div>
          <div v-if="channel.channelType === 'custom'" class="channel-credential-preview">
            <span class="label">链接地址：</span>
            <span class="value">{{ channel.customUrl || '未配置' }}</span>
          </div>
        </div>

        <div class="channel-card-actions">
          <a-button type="primary" size="small" @click="openDrawer(channel)">
            <template #icon><EditOutlined /></template>
            {{ channel.maskedCredentials ? '编辑' : '配置' }}
          </a-button>
          <a-button
            v-if="channel.maskedCredentials"
            size="small"
            :loading="validating === channel.channelType"
            @click="handleValidate(channel.channelType)"
          >
            <template #icon><ApiOutlined /></template>
            测试连接
          </a-button>
        </div>
      </div>
    </div>

    <!-- Drawer -->
    <a-drawer
      v-model:open="drawerVisible"
      :title="drawerTitle"
      width="480"
      :destroyOnClose="true"
    >
      <a-form
        :model="formState"
        :rules="formRules"
        ref="formRef"
        layout="vertical"
        @finish="handleSubmit"
      >
        <a-form-item label="渠道名称" name="channelName" :hidden="true">
          <a-input v-model:value="formState.channelName" />
        </a-form-item>

        <a-form-item label="接入方式" name="accessType">
          <a-select v-model:value="formState.accessType" @change="onAccessTypeChange">
            <a-select-option value="api_key">API Key（猎聘/拉勾）</a-select-option>
            <a-select-option value="browser_auto">浏览器自动化（BOSS/领英/小红书）</a-select-option>
            <a-select-option value="custom_url">自定义链接</a-select-option>
          </a-select>
        </a-form-item>

        <!-- API Key fields -->
        <template v-if="formState.accessType === 'api_key'">
          <a-form-item label="API Key" name="apiKey">
            <a-input v-model:value="formState.apiKey" placeholder="请输入 API Key" />
          </a-form-item>
          <a-form-item label="API Secret" name="apiSecret">
            <a-input v-model:value="formState.apiSecret" placeholder="请输入 API Secret" password />
          </a-form-item>
        </template>

        <!-- Browser Auto fields -->
        <template v-if="formState.accessType === 'browser_auto'">
          <a-form-item label="Cookie" name="cookie">
            <a-textarea
              v-model:value="formState.cookie"
              placeholder="请粘贴浏览器 Cookie"
              :rows="3"
            />
          </a-form-item>
          <a-form-item label="CSRF Token" name="csrfToken">
            <a-input v-model:value="formState.csrfToken" placeholder="可选：CSRF Token" />
          </a-form-item>
        </template>

        <!-- Custom URL -->
        <template v-if="formState.accessType === 'custom_url'">
          <a-form-item label="自定义 URL" name="customUrl">
            <a-input v-model:value="formState.customUrl" placeholder="https://..." />
          </a-form-item>
        </template>

        <a-form-item label="启用状态" name="isEnabled">
          <a-switch v-model:checked="formState.isEnabled" />
          <span style="margin-left: 8px">{{ formState.isEnabled ? '启用' : '禁用' }}</span>
        </a-form-item>

        <div class="drawer-footer">
          <a-space>
            <a-button @click="drawerVisible = false">取消</a-button>
            <a-button type="primary" html-type="submit" :loading="submitting">
              保存配置
            </a-button>
          </a-space>
        </div>
      </a-form>
    </a-drawer>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted, computed } from 'vue'
import { message } from 'ant-design-vue'
import { channelApi } from '@/api/channel'
import {
  EditOutlined,
  ApiOutlined
} from '@ant-design/icons-vue'

const loading = ref(false)
const submitting = ref(false)
const validating = ref(null)
const drawerVisible = ref(false)
const drawerTitle = ref('')
const formRef = ref(null)

const defaultChannels = [
  { channelType: 'liepin', channelName: '猎聘', accessType: 'api_key', icon: '🦊', accessTypeLabel: 'API Key' },
  { channelType: 'lagou', channelName: '拉勾', accessType: 'api_key', icon: '📋', accessTypeLabel: 'API Key' },
  { channelType: 'boss', channelName: 'Boss直聘', accessType: 'browser_auto', icon: '👔', accessTypeLabel: '浏览器自动化' },
  { channelType: 'linkedin', channelName: '领英', accessType: 'browser_auto', icon: '💼', accessTypeLabel: '浏览器自动化' },
  { channelType: 'xiaohongshu', channelName: '小红书', accessType: 'browser_auto', icon: '📕', accessTypeLabel: '浏览器自动化' },
  { channelType: 'custom', channelName: '自定义渠道', accessType: 'custom_url', icon: '🔗', accessTypeLabel: '自定义链接' }
]

const channelList = ref([...defaultChannels.map(c => ({ ...c, isEnabled: false, maskedCredentials: null, customUrl: null }))])

const formState = reactive({
  channelType: '',
  channelName: '',
  accessType: 'api_key',
  apiKey: '',
  apiSecret: '',
  cookie: '',
  csrfToken: '',
  customUrl: '',
  isEnabled: true
})

const formRules = {
  channelName: [{ required: true, message: '请输入渠道名称' }],
  accessType: [{ required: true, message: '请选择接入方式' }]
}

const onAccessTypeChange = () => {
  formState.apiKey = ''
  formState.apiSecret = ''
  formState.cookie = ''
  formState.csrfToken = ''
  formState.customUrl = ''
}

const buildCredentials = () => {
  switch (formState.accessType) {
    case 'api_key':
      return JSON.stringify({ apiKey: formState.apiKey, apiSecret: formState.apiSecret })
    case 'browser_auto':
      return JSON.stringify({ cookie: formState.cookie, csrfToken: formState.csrfToken })
    case 'custom_url':
      return JSON.stringify({ url: formState.customUrl })
    default:
      return '{}'
  }
}

const parseCredentials = (cred, accessType) => {
  if (!cred || cred === '{}') return
  try {
    const json = JSON.parse(cred)
    if (accessType === 'api_key') {
      formState.apiKey = json.apiKey || ''
      formState.apiSecret = json.apiSecret || ''
    } else if (accessType === 'browser_auto') {
      formState.cookie = json.cookie || ''
      formState.csrfToken = json.csrfToken || ''
    } else if (accessType === 'custom_url') {
      formState.customUrl = json.url || ''
    }
  } catch (e) {
    // ignore parse errors
  }
}

const openDrawer = (channel) => {
  formRef.value?.resetFields()
  Object.assign(formState, {
    channelType: channel.channelType,
    channelName: channel.channelName,
    accessType: channel.accessType,
    apiKey: '',
    apiSecret: '',
    cookie: '',
    csrfToken: '',
    customUrl: '',
    isEnabled: channel.isEnabled
  })
  parseCredentials(channel.maskedCredentials !== '*** [已加密]' ? channel.maskedCredentials : null, channel.accessType)
  drawerTitle.value = channel.maskedCredentials ? `编辑 ${channel.channelName} 凭证` : `配置 ${channel.channelName}`
  drawerVisible.value = true
}

const handleSubmit = async () => {
  submitting.value = true
  try {
    const payload = {
      channelType: formState.channelType,
      channelName: formState.channelName,
      accessType: formState.accessType,
      credentials: buildCredentials(),
      customUrl: formState.accessType === 'custom_url' ? formState.customUrl : null,
      isEnabled: formState.isEnabled
    }

    // Check if channel exists
    const existing = channelList.value.find(c => c.channelType === formState.channelType)
    if (existing?.maskedCredentials) {
      await channelApi.update(formState.channelType, payload)
      message.success('更新成功')
    } else {
      await channelApi.create(payload)
      message.success('创建成功')
    }

    drawerVisible.value = false
    await loadChannels()
  } catch (err) {
    message.error(err.message || '保存失败')
  } finally {
    submitting.value = false
  }
}

const handleValidate = async (channelType) => {
  validating.value = channelType
  try {
    const res = await channelApi.validate(channelType)
    if (res.success) {
      message.success(res.message)
    } else {
      message.warning(res.message)
    }
  } catch (err) {
    message.error('连接测试失败')
  } finally {
    validating.value = null
  }
}

const loadChannels = async () => {
  loading.value = true
  try {
    const res = await channelApi.list()
    const items = res.data?.items || []
    channelList.value = defaultChannels.map(def => {
      const saved = items.find(i => i.channelType === def.channelType)
      if (saved) {
        return { ...def, ...saved }
      }
      return { ...def, isEnabled: false, maskedCredentials: null, customUrl: null }
    })
  } catch (err) {
    console.error('Failed to load channels:', err)
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  loadChannels()
})
</script>

<style scoped>
.channel-credential-management {
  padding: 24px;
}

.page-header {
  margin-bottom: 24px;
}

.page-header h1 {
  font-size: 20px;
  font-weight: 600;
  color: #1F2937;
  margin: 0 0 4px 0;
}

.page-header p {
  color: #6B7280;
  font-size: 14px;
  margin: 0;
}

.channel-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
  gap: 16px;
}

.channel-card {
  background: #fff;
  border: 1px solid #E5E7EB;
  border-radius: 8px;
  padding: 20px;
  transition: all 0.2s;
}

.channel-card:hover {
  border-color: #0D3D92;
  box-shadow: 0 2px 8px rgba(13, 61, 146, 0.1);
}

.channel-disabled {
  opacity: 0.7;
}

.channel-card-header {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 16px;
}

.channel-icon {
  font-size: 28px;
  line-height: 1;
}

.channel-info {
  flex: 1;
}

.channel-name {
  font-size: 16px;
  font-weight: 600;
  color: #1F2937;
}

.channel-type-badge {
  font-size: 12px;
  color: #6B7280;
  margin-top: 2px;
}

.channel-card-body {
  margin-bottom: 16px;
}

.channel-credential-preview {
  display: flex;
  gap: 8px;
  font-size: 13px;
  margin-bottom: 6px;
}

.channel-credential-preview .label {
  color: #9CA3AF;
  flex-shrink: 0;
}

.channel-credential-preview .value {
  color: #4B5563;
  font-family: monospace;
  word-break: break-all;
}

.channel-card-actions {
  display: flex;
  gap: 8px;
}

.drawer-footer {
  padding-top: 16px;
  border-top: 1px solid #E5E7EB;
  margin-top: 24px;
}
</style>
