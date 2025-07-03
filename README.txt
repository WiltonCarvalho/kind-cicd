KUBECTL_VERSION=$(curl -L -s https://dl.k8s.io/release/stable-1.32.txt)
sudo curl -fsSL https://dl.k8s.io/release/$KUBECTL_VERSION/bin/linux/amd64/kubectl -o /usr/local/bin/kubectl
sudo chmod +x /usr/local/bin/kubectl

HELM_VERSION=$(curl -s https://api.github.com/repos/helm/helm/releases/latest | grep '"tag_name":' | sed -E 's/.*"tag_name": "([^"]+)".*/\1/')
curl -fsSL https://get.helm.sh/helm-$HELM_VERSION-linux-amd64.tar.gz | sudo tar zxvf - -C "/usr/local/bin" linux-amd64/helm --strip-components 1

KUBECTX_VERSION=$(curl -s https://api.github.com/repos/ahmetb/kubectx/releases/latest | grep '"tag_name":' | sed -E 's/.*"tag_name": "([^"]+)".*/\1/')
sudo curl -fsSL https://github.com/ahmetb/kubectx/releases/download/$KUBECTX_VERSION/kubectx -o /usr/local/bin/kubectx
sudo chmod +x /usr/local/bin/kubectx

YQ_VERSION=$(curl -s https://api.github.com/repos/mikefarah/yq/releases/latest | grep '"tag_name":' | sed -E 's/.*"tag_name": "([^"]+)".*/\1/')
sudo curl -fsSL https://github.com/mikefarah/yq/releases/download/$YQ_VERSION/yq_linux_amd64 -o /usr/local/bin/yq
sudo chmod +x /usr/local/bin/yq

KUSTOMIZE_VERSION=$(curl -s https://api.github.com/repos/kubernetes-sigs/kustomize/releases/latest | grep '"tag_name":' | sed -E 's/.*"tag_name": "([^"]+)".*/\1/')
curl -fsSL https://github.com/kubernetes-sigs/kustomize/releases/download/${KUSTOMIZE_VERSION}/kustomize_${KUSTOMIZE_VERSION#kustomize/}_linux_amd64.tar.gz | sudo tar zxvf - -C "/usr/local/bin"

KIND_VERSION=$(curl -s https://api.github.com/repos/kubernetes-sigs/kind/releases/latest | grep '"tag_name":' | sed -E 's/.*"tag_name": "([^"]+)".*/\1/')
sudo curl -fsSL https://github.com/kubernetes-sigs/kind/releases/download/$KIND_VERSION/kind-linux-amd64 -o /usr/local/bin/kind
sudo chmod +x /usr/local/bin/kind

kind delete cluster || true
docker network rm kind || true
docker network create kind --subnet 172.31.0.0/16

K8S_VERSION=$(skopeo list-tags docker://docker.io/kindest/node | jq -r '.Tags[]' | grep ^v1.32 | sort -V | tail -1)
kind create cluster --image kindest/node:$K8S_VERSION --config kind/kind-config.yaml

kubectl version
kubectl get node
helm version

helm upgrade --install metrics-server metrics-server --repo https://kubernetes-sigs.github.io/metrics-server --namespace kube-system \
--set "args={--kubelet-insecure-tls,--kubelet-preferred-address-types=InternalIP}"

METALLB_VERSION=$(curl -s https://api.github.com/repos/metallb/metallb/releases/latest | grep '"tag_name":' | sed -E 's/.*"tag_name": "([^"]+)".*/\1/')
kubectl apply -f https://raw.githubusercontent.com/metallb/metallb/$METALLB_VERSION/config/manifests/metallb-native.yaml
kubectl -n metallb-system wait --for=condition=ready pod -l app=metallb

kubectl apply -f https://github.com/WiltonCarvalho/kubernetes/raw/main/kind-metal-lb-nginx-ingress/metallb-config.yaml
kubectl -n metallb-system get ipaddresspools

kubectl run httpd --image httpd:alpine --port 80 --labels app=httpd
kubectl wait --for=condition=ready pod -l app=httpd
kubectl expose pod httpd --name=httpd --type=LoadBalancer --load-balancer-ip=172.31.255.25 --labels app=httpd
sleep 3
curl 172.31.255.25

openssl req -x509 -nodes -days 3650 -new -subj "/CN=wiltoncarvalho.com" \
  -newkey rsa:2048 -keyout /tmp/key.pem -out /tmp/cert.pem

kubectl create secret tls default-ssl-secret \
  --key /tmp/key.pem --cert /tmp/cert.pem --namespace default

helm upgrade --install ingress-nginx ingress-nginx \
  --repo https://kubernetes.github.io/ingress-nginx \
  --namespace ingress-nginx --create-namespace \
  --version 4.12.1 \
  --set controller.service.type=LoadBalancer \
  --set controller.service.loadBalancerIP=172.31.255.254 \
  --set controller.extraArgs.default-ssl-certificate='default/default-ssl-secret'

kubectl -n ingress-nginx wait --for=condition=ready pod -l "app.kubernetes.io/name=ingress-nginx"
sleep 3
curl -I 172.31.255.254/healthz

kubectl create ingress httpd --class=nginx --rule "httpd.wiltoncarvalho.com/*=httpd:80"
sleep 3
curl http://httpd.wiltoncarvalho.com --resolve httpd.wiltoncarvalho.com:80:172.31.255.254
curl https://httpd.wiltoncarvalho.com --resolve httpd.wiltoncarvalho.com:443:172.31.255.254 -k

kubectl apply -f kind/registry.yaml
kubectl wait --for=condition=ready pod -l app=registry
curl -i http://registry.172.31.255.201.sslip.io

skopeo copy --dest-tls-verify=false docker://nginx:alpine docker://registry.172.31.255.201.sslip.io/nginx:alpine

kubectl create deployment nginx --image registry.172.31.255.201.sslip.io/nginx:alpine --port 80
kubectl wait --for=condition=ready pod -l app=nginx
kubectl patch deployment nginx --type='json' -p='[{"op": "replace", "path": "/spec/template/spec/containers/0/image", "value":"localhost:32000/nginx:alpine"}]'
kubectl wait --for=condition=ready pod -l app=nginx

helm upgrade --install argocd argo-cd --repo https://argoproj.github.io/argo-helm \
--namespace argocd --values argocd/argocd-values.yaml --create-namespace

kubectl -n argocd wait --for=condition=ready pod -l "app.kubernetes.io/part-of=argocd"

kubectl -n argocd get secret argocd-initial-admin-secret -o jsonpath="{.data.password}" | base64 -d && echo

google-chrome http://argocd.172.31.255.254.sslip.io

kubectl apply -k gitea

google-chrome http://gitea.172.31.255.254.sslip.io

kubectl exec -it gitea-0 -- su-exec git /usr/local/bin/gitea admin user generate-access-token --username root --token-name root --scopes all

09c7aa2a5b64183bab99de4ddaca1d6ab009badb

(
  mkdir -p /tmp/whoami
  cp kind/whoami.yaml /tmp/whoami/
  cd /tmp/whoami
  echo '# whoami' > readme.md
  git config --global user.email "you@example.com"
  git config --global user.name "Your Name"
  git init --initial-branch=main
  git remote rm origin || true
  git remote add origin http://root:09c7aa2a5b64183bab99de4ddaca1d6ab009badb@gitea.172.31.255.254.sslip.io/root/whoami
  git add readme.md
  git add whoami.yaml
  git commit -m "Initial commit"
  git push --set-upstream origin main
)

add gitea token to argocd/argocd-creds.yaml

kubectl apply -f argocd/argocd-creds.yaml
kubectl apply -f argocd/argocd-application.yaml

google-chrome http://gitea.172.31.255.254.sslip.io/admin/applications
Drone Oauth2 App
Redirect URI: http://drone.172.31.255.254.sslip.io/login
Add Secret to drone/drone.env

kubectl apply -k drone

google-chrome drone.172.31.255.254.sslip.io

