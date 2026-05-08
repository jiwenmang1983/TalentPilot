<template>
  <div class="user-management">
    <div class="toolbar">
      <a-space>
        <a-input
          v-model:value="searchKeyword"
          placeholder="搜索用户名/邮箱"
          style="width: 200px"
          allow-clear
          @search="handleSearch"
        >
          <template #prefix><SearchOutlined /></template>
        </a-input>
        <a-select
          v-model:value="filterRole"
          placeholder="选择角色"
          style="width: 150px"
          allow-clear
          @change="handleSearch"
        >
          <a-select-option v-for="role in roles" :key="role.id" :value="role.id">
            {{ role.RoleName }}
          </a-select-option>
        </a-select>
        <a-select
          v-model:value="filterIsActive"
          placeholder="状态"
          style="width: 120px"
          allow-clear
          @change="handleSearch"
        >
          <a-select-option :value="true">启用</a-select-option>
          <a-select-option :value="false">禁用</a-select-option>
        </a-select>
        <a-button type="primary" @click="handleSearch">
          <template #icon><SearchOutlined /></template>
          搜索
        </a-button>
      </a-space>
      <a-button type="primary" @click="openDrawer()">
        <template #icon><PlusOutlined /></template>
        新建用户
      </a-button>
    </div>

    <a-table
      :columns="columns"
      :data-source="users"
      :loading="loading"
      :pagination="pagination"
      @change="handleTableChange"
      row-key="id"
    >
      <template #bodyCell="{ column, record }">
        <template v-if="column.key === 'status'">
          <a-tag :color="record.isActive ? 'green' : 'red'">
            {{ record.isActive ? '启用' : '禁用' }}
          </a-tag>
        </template>
        <template v-else-if="column.key === 'roleName'">
          <a-tag>{{ record.roleName || '—' }}</a-tag>
        </template>
        <template v-else-if="column.key === 'department'">
          <span>{{ record.departmentName || '—' }}</span>
        </template>
        <template v-else-if="column.key === 'actions'">
          <a-space>
            <a-button type="link" size="small" @click="openDrawer(record)">
              编辑
            </a-button>
            <a-button
              type="link"
              size="small"
              :style="{ color: record.isActive ? '#ff4d4f' : '#52c41a' }"
              @click="toggleStatus(record)"
            >
              {{ record.isActive ? '禁用' : '启用' }}
            </a-button>
            <a-button type="link" size="small" danger @click="resetPassword(record)">
              重置密码
            </a-button>
          </a-space>
        </template>
      </template>
    </a-table>

    <a-drawer
      v-model:open="drawerVisible"
      :title="drawerTitle"
      width="500"
      @close="closeDrawer"
    >
      <a-form
        :model="formState"
        :rules="formRules"
        ref="formRef"
        layout="vertical"
      >
        <a-form-item label="用户名" name="username">
          <a-input v-model:value="formState.username" placeholder="请输入用户名" />
        </a-form-item>
        <a-form-item label="邮箱" name="email">
          <a-input v-model:value="formState.email" placeholder="请输入邮箱" />
        </a-form-item>
        <a-form-item label="密码" name="password" v-if="!isEdit">
          <a-input-password v-model:value="formState.password" placeholder="请输入密码" />
        </a-form-item>
        <a-form-item label="角色" name="roleId">
          <a-select
            v-model:value="formState.roleId"
            placeholder="请选择角色"
          >
            <a-select-option v-for="role in roles" :key="role.id" :value="role.id">
              {{ role.roleName }}
            </a-select-option>
          </a-select>
        </a-form-item>
        <a-form-item label="部门" name="departmentId">
          <a-tree-select
            v-model:value="formState.departmentId"
            :tree-data="departmentTree"
            placeholder="请选择部门"
            tree-default-expand-all
          />
        </a-form-item>
      </a-form>
      <template #footer>
        <a-space>
          <a-button @click="closeDrawer">取消</a-button>
          <a-button type="primary" :loading="submitLoading" @click="handleSubmit">
            确定
          </a-button>
        </a-space>
      </template>
    </a-drawer>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted, computed } from 'vue'
import { message, Modal } from 'ant-design-vue'
import { SearchOutlined, PlusOutlined } from '@ant-design/icons-vue'
import { userApi, roleApi, departmentApi } from '@/api'
import dayjs from 'dayjs'

const users = ref([])
const roles = ref([])
const departmentTree = ref([])
const loading = ref(false)
const drawerVisible = ref(false)
const drawerTitle = ref('新建用户')
const isEdit = ref(false)
const formRef = ref(null)
const submitLoading = ref(false)

const searchKeyword = ref('')
const filterRole = ref(null)
const filterIsActive = ref(null)

const pagination = reactive({
  current: 1,
  pageSize: 20,
  total: 0,
  showSizeChanger: true,
  showTotal: (total) => `共 ${total} 条`
})

const columns = [
  { title: 'ID', dataIndex: 'id', key: 'id', width: 80 },
  { title: '用户名', dataIndex: 'username', key: 'username' },
  { title: '邮箱', dataIndex: 'email', key: 'email' },
  { title: '角色', key: 'roleName' },
  { title: '部门', key: 'department' },
  { title: '状态', key: 'status', width: 100 },
  { title: '创建时间', dataIndex: 'createdAt', key: 'createdAt', width: 180, customRender: ({ text }) => text ? dayjs(text).format('YYYY-MM-DD HH:mm') : '—' },
  { title: '操作', key: 'actions', width: 220, fixed: 'right' }
]

const formState = reactive({
  id: null,
  username: '',
  email: '',
  password: '',
  roleId: null,   // API用RoleId单数
  departmentId: null
})

const formRules = {
  username: [{ required: true, message: '请输入用户名', trigger: 'blur' }],
  email: [
    { required: true, message: '请输入邮箱', trigger: 'blur' },
    { type: 'email', message: '请输入正确的邮箱格式', trigger: 'blur' }
  ],
  password: [{ required: true, message: '请输入密码', trigger: 'blur' }]
}

function transformTree(nodes) {
  if (!nodes) return []
  return nodes.map(node => ({
    value: node.id,
    label: node.departmentName,   // API返回departmentName
    children: transformTree(node.children)
  }))
}

async function fetchUsers() {
  loading.value = true
  try {
    const params = {
      page: pagination.current,
      pageSize: pagination.pageSize,
      roleId: filterRole.value || undefined,
      isActive: filterIsActive.value !== null ? filterIsActive.value : undefined
    }
    if (searchKeyword.value) {
      // Simple search via username or email - backend doesn't have search yet, but we can filter client-side
      params.keyword = searchKeyword.value
    }
    const res = await userApi.list(params)
    const data = res.data || res
    users.value = data.items || data.Items || []
    pagination.total = data.total || data.Total || 0
  } catch (e) {
    message.error('获取用户列表失败')
  } finally {
    loading.value = false
  }
}

async function fetchRoles() {
  try {
    const res = await roleApi.list()
    roles.value = res.data || res || []
  } catch (e) {
    console.error('Failed to fetch roles:', e)
  }
}

async function fetchDepartments() {
  try {
    const res = await departmentApi.getTree()
    departmentTree.value = transformTree(res.data || res || [])
  } catch (e) {
    console.error('Failed to fetch departments:', e)
  }
}

function handleSearch() {
  pagination.current = 1
  fetchUsers()
}

function handleTableChange(pag) {
  pagination.current = pag.current
  pagination.pageSize = pag.pageSize
  fetchUsers()
}

function openDrawer(record = null) {
  if (record) {
    isEdit.value = true
    drawerTitle.value = '编辑用户'
    formState.id = record.id
    formState.username = record.username
    formState.email = record.email
    formState.roleId = record.roleId || null   // API返回单roleId
    formState.departmentId = record.departmentId
    delete formRules.password
  } else {
    isEdit.value = false
    drawerTitle.value = '新建用户'
    Object.assign(formState, {
      id: null,
      username: '',
      email: '',
      password: '',
      roleId: null,   // 单选角色
      departmentId: null
    })
    formRules.password = [{ required: true, message: '请输入密码', trigger: 'blur' }]
  }
  drawerVisible.value = true
}

function closeDrawer() {
  drawerVisible.value = false
  formRef.value?.resetFields()
}

async function handleSubmit() {
  try {
    await formRef.value.validate()
    submitLoading.value = true

    if (isEdit.value) {
      await userApi.update(formState.id, {
        email: formState.email,
        roleId: formState.roleId,   // API用RoleId单数
        departmentId: formState.departmentId
      })
      message.success('更新成功')
    } else {
      await userApi.create({
        username: formState.username,
        email: formState.email,
        password: formState.password,
        roleId: formState.roleId,   // API用RoleId单数
        departmentId: formState.departmentId
      })
      message.success('创建成功')
    }

    closeDrawer()
    fetchUsers()
  } catch (e) {
    if (e?.message && !e?.response?.data?.message) {
      message.error(e.message)
    }
  } finally {
    submitLoading.value = false
  }
}

async function toggleStatus(record) {
  const action = record.status === 'active' ? '禁用' : '启用'
  Modal.confirm({
    title: `确认${action}`,
    content: `确定要${action}用户 ${record.username} 吗？`,
    onOk: async () => {
      try {
        if (record.isActive) {
          await userApi.toggleActive(record.id)
        } else {
          await userApi.toggleActive(record.id)
        }
        message.success(`${action}成功`)
        fetchUsers()
      } catch (e) {
        message.error(`${action}失败`)
      }
    }
  })
}

async function resetPassword(record) {
  Modal.confirm({
    title: '重置密码',
    content: `确定要重置用户 ${record.username} 的密码吗？`,
    onOk: async () => {
      try {
        await userApi.resetPassword(record.id)
        message.success('密码已重置')
      } catch (e) {
        message.error('重置密码失败')
      }
    }
  })
}

onMounted(() => {
  fetchUsers()
  fetchRoles()
  fetchDepartments()
})
</script>

<style scoped>
.user-management {
  height: 100%;
}

.toolbar {
  display: flex;
  justify-content: space-between;
  margin-bottom: 16px;
}
</style>